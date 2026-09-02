#if !MAUI
using GA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TGL;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace RubikCube
{
    // GPU compute pipeline for the genetic algorithm: shader programs, SSBO/UBO buffers and the GA
    // loop. Everything is static and shared - it belongs to no particular render window (TGLContext).
    // Init() must be called once while a GL context is current (TGLContext.Handle does that).
    public static unsafe class Gpu
    {
        public static uint InitProgram, EvaluateMicroProgram, EvaluateMacroProgram, SortProgram, SelCrossoverProgram;
        public static Ssbo Population, Children;   // parents (binding 0) / crossover output (binding 1); ping-ponged in ExecuteGA
        public static Ssbo CubiesBuffer, FreeMovesBuffer, SolvedBuffer, ActiveBuffer, PlanesBuffer, SeedMovesBuffer;
        static string Setup;
        static int CompiledN, CompiledSize;
        static uint NumSeeds;   // specimens pre-seeded by Init (host-built SeedMoves)
        public static int GenerationsCount;


        // One-time GPU setup: reserve the buffers, create the program + shader objects (attached but
        // not yet compiled), then compile them for the current cube. Call once with a current GL context.
        public static void Init()
        {
            Setup = ReadManifestText("Resources.Setup.glsl.c");
            Setup = Setup.Replace("#define uint unsigned int", "");

            // Reserve the buffers once, in binding order (0..7 as declared in Setup.glsl.c). The
            // constructor only reserves id + binding; CreateBuffers sizes and fills them per run via Update.
            Ssbo.ResetBinding();
            Population = new Ssbo();                                // 0
            Children   = new Ssbo();                                // 1
            CubiesBuffer    = new Ssbo();                           // 2
            FreeMovesBuffer = new Ssbo();                           // 3
            SolvedBuffer    = new Ssbo();                           // 4
            ActiveBuffer    = new Ssbo();                           // 5
            PlanesBuffer    = new Ssbo(OpenGL.GL_UNIFORM_BUFFER);   // 6  std140 UBO
            SeedMovesBuffer = new Ssbo();                           // 7  Init seed sequences

            // Create the program + shader objects once and attach them. BuildShaders only (re)sources,
            // compiles and links these same objects, so nothing is ever created or deleted again.
            InitProgram = CreateProgram();
            EvaluateMicroProgram = CreateProgram();
            EvaluateMacroProgram = CreateProgram();
            SortProgram = CreateProgram();
            SelCrossoverProgram = CreateProgram();

            BuildShaders();
        }

        static int GetDefineValue(string defineName)
        {
            string targetToken = "#define " + defineName;
            using var reader = new StringReader(Setup);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.StartsWith(targetToken, StringComparison.Ordinal))
                {
                    string valuePart = line.Substring(targetToken.Length).Trim();
                    if (int.TryParse(valuePart, out int result))
                        return result;
                }
            }
            return -1;
        }

        static void SetDefineValue(string defineName, int value)
        {
            var oldValue = GetDefineValue(defineName);
            var defineLine = "#define " + defineName + " ";
            Setup = Setup.Replace(defineLine + oldValue, defineLine + value);
        }

        // Injects N/SIZE from the current cube as compile-time defines (the shader arrays are sized by
        // them) and (re)compiles + links the existing programs so they match the cube. Called from Init
        // and again by CreateBuffers whenever N/SIZE change. Population / genes / generations stay as
        // authored in Setup.glsl.c.
        public static void BuildShaders()
        {
            SetDefineValue("N", TAffine.N);
            SetDefineValue("SIZE", TRubikCube.Size);
            SetDefineValue("CUBIES_COUNT", (int)Math.Pow(TRubikCube.Size, TAffine.N));
            TChromosome.GenesLength = GetDefineValue("GENES_COUNT");
            TGA<TRubikGenome>.PopulationCount = GetDefineValue("POPULATION_COUNT");
            TGA<TRubikGenome>.GenerationsCount = GetDefineValue("GENERATIONS_COUNT");
            TGA<TRubikGenome>.StallLimit = GetDefineValue("STALL_LIMIT");
            CompiledN = TAffine.N;
            CompiledSize = TRubikCube.Size;

            CompileProgram(InitProgram, "Resources.Init.glsl.c");
            CompileProgram(EvaluateMicroProgram, "Resources.EvaluateMicro.glsl.c");
            CompileProgram(EvaluateMacroProgram, "Resources.EvaluateMacro.glsl.c");
            CompileProgram(SortProgram, "Resources.Sort.glsl.c");
            CompileProgram(SelCrossoverProgram, "Resources.SelCrossover.glsl.c");
        }

        static string ReadManifestText(string dotPath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string rootNamespace = assembly.GetName().Name;
            string resourceName = $"{rootNamespace}.{dotPath}";
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        // Create a compute program and its shader and attach them (once, in Init). Sourcing, compiling
        // and linking happen later in BuildShaders, so an N/SIZE change just recompiles these objects.
        static uint CreateProgram()
        {
            var program = OpenGL.CreateProgram();
            var shader = OpenGL.CreateShader(OpenGL.GL_COMPUTE_SHADER);
            OpenGL.AttachShader(program, shader);
            return program;
        }

        // (Re)source the program's single attached shader with the current Setup header, compile it and
        // relink. The objects already exist (created in Init); the shader handle is read back with
        // glGetAttachedShaders rather than cached, so the class keeps only the program handles.
        static void CompileProgram(uint program, string dotPath)
        {
            uint shader;
            int count;
            OpenGL.GetAttachedShaders(program, 1, &count, &shader);
            var code = ReadManifestText(dotPath).Replace("#include \"Setup.glsl.c\"", Setup);
            OpenGL.ShaderSource(shader, code);
            OpenGL.CompileShader(shader);
            int status;
            OpenGL.GetShaderiv(shader, OpenGL.GL_COMPILE_STATUS, &status);
            if (status == 0)
                throw new Exception(OpenGL.GetShaderInfoLog(shader));
            OpenGL.LinkProgram(program);
        }

        // Uploads the per-run data into the buffers reserved in Init (0 Population, 1 NewPopulation,
        // 2 Cubies, 3 FreeMoves, 4 SolvedCubies, 5 ActiveCubies, 6 Planes UBO). Update reuses the same
        // ids/bindings and resizes the store as needed, so there is no buffer churn between GA runs.
        public static void CreateBuffers(TRubikCube cube, List<int> freeMoves)
        {
            // Keep the shader in sync with the cube: N/SIZE are compile-time defines, so recompile
            // when the dimension or size changed, otherwise the shader reads Cubies out of bounds.
            if (CompiledN != TAffine.N || CompiledSize != TRubikCube.Size)
                BuildShaders();
            var populationSize = TGA<TRubikGenome>.PopulationCount * (TChromosome.GenesLength + 2) * sizeof(int);
            var cubies = cube.PackCubies();
            var moves = freeMoves.ToArray();
            // Pad empty index arrays to one element so we never make a zero-size SSBO (unreliable
            // .length() / binding). The countSolved / countActive uniforms gate the loops.
            var solved = cube.SolvedCubies.Select(c => c.StartIndex).ToArray();
            if (solved.Length == 0) solved = new int[1];
            var active = cube.ActiveCluster.Select(c => c.StartIndex).ToArray();
            if (active.Length == 0) active = new int[1];
            // std140 array of ivec2: each element is padded to 16 bytes (vec4 alignment).
            var planes = new int[TAffine.Planes.Length * 4];
            for (int i = 0; i < TAffine.Planes.Length; i++)
            {
                planes[4 * i] = TAffine.Planes[i][0];
                planes[4 * i + 1] = TAffine.Planes[i][1];
            }

            Population.Update(populationSize, null);
            Children.Update(populationSize, null);
            fixed (uint* p = cubies) CubiesBuffer.Update(cubies.Length * sizeof(uint), p);
            fixed (int* p = moves) FreeMovesBuffer.Update(moves.Length * sizeof(int), p);
            fixed (int* p = solved) SolvedBuffer.Update(solved.Length * sizeof(int), p);
            fixed (int* p = active) ActiveBuffer.Update(active.Length * sizeof(int), p);
            fixed (int* p = planes) PlanesBuffer.Update(planes.Length * sizeof(int), p);

            // Per-specimen seed sequences for Init: reverse-transform moves that undo active-cluster
            // cubies. SEED_RATIO % of the population is seeded (rest random); stride = N-1 genes.
            var seedMoves = cube.BuildSeedMoves(TGA<TRubikGenome>.PopulationCount, TAffine.N - 1,
                                                GetDefineValue("SEED_RATIO"), out NumSeeds);
            fixed (int* p = seedMoves) SeedMovesBuffer.Update(seedMoves.Length * sizeof(int), p);
        }

        // Scores the current cube on the GPU with the SAME evaluator the GA uses: a zero specimen (all
        // genes 0 = 0-degree identity moves after the quarter-turn encoding) leaves the cube unchanged,
        // so the evaluator returns baseline + no-op penalty. We subtract the penalty - MEASURED, not
        // hardcoded, as the same zero specimen's fitness on a SOLVED cube (baseline == 0, so the fitness
        // is exactly the penalty). One scorer (the evaluator), no separate kernel, no magic constant,
        // and the baseline tracks any future change to the no-op penalty automatically.
        static float noOpPenalty = float.NaN;
        static int penaltyN, penaltySize;
        public static float ScoreCube(TRubikCube cube)
        {
            if (CompiledN != TAffine.N || CompiledSize != TRubikCube.Size)
                BuildShaders();

            if (float.IsNaN(noOpPenalty) || penaltyN != TAffine.N || penaltySize != TRubikCube.Size)
            {
                // penalty = zero specimen on a solved cube (all identity-packed): baseline 0 -> fitness == penalty.
                int bitsPerRow = (TAffine.N <= 4 ? 2 : 3) + 1;
                uint id = 0;
                for (int r = 0; r < TAffine.N; r++) id |= (uint)r << (r * bitsPerRow);
                var solvedCubies = new uint[cube.Cubies.Length];
                Array.Fill(solvedCubies, id);
                noOpPenalty = EvalZeroSpecimen(cube, solvedCubies);
                penaltyN = TAffine.N; penaltySize = TRubikCube.Size;
            }
            return EvalZeroSpecimen(cube, cube.PackCubies()) - noOpPenalty;
        }

        // Uploads `cubies` + the cube's active/solved cluster, evaluates ONE all-zero specimen (identity
        // moves) and returns its fitness = the cube's raw score PLUS the no-op penalty (a zero specimen
        // never changes the active cluster, so the penalty always fires).
        static float EvalZeroSpecimen(TRubikCube cube, uint[] cubies)
        {
            var micro = cube.Cubies.Length <= 64;
            var eval = micro ? EvaluateMicroProgram : EvaluateMacroProgram;

            var spec = new int[TChromosome.GenesLength + 2];   // all zero = identity moves
            fixed (int* p = spec) Population.Update(spec.Length * sizeof(int), p);
            fixed (uint* p = cubies) CubiesBuffer.Update(cubies.Length * sizeof(uint), p);

            var solved = cube.SolvedCubies.Select(c => c.StartIndex).ToArray();
            if (solved.Length == 0) solved = new int[1];
            var active = cube.ActiveCluster.Select(c => c.StartIndex).ToArray();
            if (active.Length == 0) active = new int[1];
            fixed (int* p = solved) SolvedBuffer.Update(solved.Length * sizeof(int), p);
            fixed (int* p = active) ActiveBuffer.Update(active.Length * sizeof(int), p);
            var planes = new int[TAffine.Planes.Length * 4];       // evaluator's TurnSingleCubie reads Planes[] (std140 ivec2)
            for (int i = 0; i < TAffine.Planes.Length; i++)
            { planes[4 * i] = TAffine.Planes[i][0]; planes[4 * i + 1] = TAffine.Planes[i][1]; }
            fixed (int* p = planes) PlanesBuffer.Update(planes.Length * sizeof(int), p);

            OpenGL.UseProgram(eval);
            OpenGL.Uniform1ui(4, (uint)cube.SolvedCubies.Count);   // countSolved
            OpenGL.Uniform1ui(5, (uint)cube.ActiveCluster.Count);  // countActive
            OpenGL.BindBufferBase(Population.Type, 0, Population.Id);
            OpenGL.DispatchCompute(1, 1, 1);                        // one specimen: 1 thread (micro) / 1 block (macro)
            OpenGL.MemoryBarrier(OpenGL.MemoryBarrierFlags.ShaderStorage);

            var result = new int[1];
            OpenGL.BindBuffer(Population.Type, Population.Id);
            fixed (int* rp = result)
                OpenGL.GetBufferSubData(Population.Type, 0, sizeof(float), rp);
            return BitConverter.Int32BitsToSingle(result[0]);
        }

        // Runs the genetic algorithm on the GPU for the current cube (TRubikGenome.RubikCube /
        // TRubikGenome.FreeMoves) and returns the best specimen found.
        public static TRubikGenome ExecuteGA()
        {
            var cube = TRubikGenome.RubikCube;
            var populationCount = (uint)TGA<TRubikGenome>.PopulationCount;
            var micro = cube.Cubies.Length <= 64;
            var evalProgram = micro ? EvaluateMicroProgram : EvaluateMacroProgram;

            // Create + upload all buffers for this run (population, packed cube, free moves, indices, planes).
            CreateBuffers(cube, TRubikGenome.FreeMoves);
            var solvedCount = (uint)cube.SolvedCubies.Count;
            var activeCount = (uint)cube.ActiveCluster.Count;

            // Dispatch dims. Init/SelCrossover: 1 thread per specimen (blocks of 64).
            // Evaluate: micro = 1 thread/specimen; macro = 1 block (256 threads) per specimen.
            uint genGroups = (populationCount + 63) / 64;
            uint evalGroups = micro ? genGroups : populationCount;

            int parentSlot = 0;
            var best = new TRubikGenome();
            var specimenSize = (TChromosome.GenesLength + 2) * sizeof(int);
            var bestBuffer = new int[specimenSize / sizeof(int)];
            fixed (int* bestBufPtr = bestBuffer)
            {
                // Init: fill the population (binding 0) with random valid move sequences.
                uint timeSeed = (uint)Random.Shared.Next() | (Random.Shared.Next(2) == 0 ? 0x80000000u : 0u);
                OpenGL.UseProgram(InitProgram);
                Population.Bind();
                OpenGL.Uniform1ui(0, timeSeed);                 // TimeSeed
                OpenGL.Uniform1ui(6, NumSeeds);                 // numSeeds
                OpenGL.DispatchCompute(genGroups, 1, 1);
                OpenGL.MemoryBarrier(OpenGL.MemoryBarrierFlags.ShaderStorage);

                var pop = new[] { Population, Children };   // local ping-pong pair (binding 0/1 swap each gen)
                for (GenerationsCount = 0; GenerationsCount < TGA<TRubikGenome>.GenerationsCount; GenerationsCount++)
                {
                    var parent = pop[parentSlot];
                    var child = pop[parentSlot ^ 1];

                    // 1. Evaluate parents (binding 0). Cluster sizes come from the host, not .length().
                    OpenGL.UseProgram(evalProgram);
                    OpenGL.Uniform1ui(4, solvedCount);
                    OpenGL.Uniform1ui(5, activeCount);
                    OpenGL.BindBufferBase(parent.Type, 0, parent.Id);
                    OpenGL.DispatchCompute(evalGroups, 1, 1);
                    OpenGL.MemoryBarrier(OpenGL.MemoryBarrierFlags.ShaderStorage);

                    // 2. Bitonic sort: smallest fitness ends up at index 0. u_Stage=2, u_PassModStage=3.
                    OpenGL.UseProgram(SortProgram);
                    for (uint stage = 2; stage <= populationCount; stage <<= 1)
                    {
                        OpenGL.Uniform1ui(2, stage);
                        for (uint passModStage = stage >> 1; passModStage > 0; passModStage >>= 1)
                        {
                            OpenGL.Uniform1ui(3, passModStage);
                            OpenGL.DispatchCompute(populationCount / 256, 1, 1);
                            OpenGL.MemoryBarrier(OpenGL.MemoryBarrierFlags.ShaderStorage);
                        }
                    }

                    // 3. Read the best specimen (Population[parentSlot][0] after the sort).
                    OpenGL.BindBuffer(parent.Type, parent.Id);
                    OpenGL.GetBufferSubData(parent.Type, 0, specimenSize, bestBufPtr);
                    var fitness = BitConverter.UInt32BitsToSingle((uint)bestBuffer[0]);
                    if (fitness < best.Fitness)
                    {
                        best.ReadBuffer(bestBuffer);
                        if (best.Fitness < cube.Score)
                            return best;
                    }

                    // 4. Selection + crossover: parents at binding 0, children written to binding 1.
                    OpenGL.UseProgram(SelCrossoverProgram);
                    OpenGL.BindBufferBase(parent.Type, 0, parent.Id);
                    OpenGL.BindBufferBase(child.Type, 1, child.Id);
                    OpenGL.Uniform1ui(0, timeSeed++);           // TimeSeed
                    OpenGL.Uniform1ui(6, NumSeeds);             // numSeeds
                    OpenGL.DispatchCompute(genGroups, 1, 1);
                    OpenGL.MemoryBarrier(OpenGL.MemoryBarrierFlags.ShaderStorage);

                    // 5. Ping-pong: this generation's children are next generation's parents.
                    parentSlot ^= 1;
                }
                //// 6. Read the second best specimen
                //var popBuffer = PopulationBuffer[parentSlot ^ 1];
                //OpenGL.BindBuffer(popBuffer.Type, popBuffer.Id);
                //OpenGL.GetBufferSubData(popBuffer.Type, specimenSize, specimenSize, bestBufPtr);
                //best.ReadBuffer(bestBuffer);
            }
            return best;
        }
    }
}
#endif
