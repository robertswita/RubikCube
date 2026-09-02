using GA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using TGL;

namespace RubikCube
{
    // GPU compute pipeline for the genetic algorithm: shader programs, SSBO/UBO buffers and the GA
    // loop. Everything is static and shared - it belongs to no particular render window (TGLContext).
    // Init() must be called once while a GL context is current (TGLContext.Create does that).
    public static unsafe class Gpu
    {
        public static uint InitProgram, EvaluateMicroProgram, EvaluateMacroProgram, SortProgram, SelCrossoverProgram, ReplayWinnerProgram;
        public static Ssbo Population, Children;   // parents (binding 0) / crossover output (binding 1); ping-ponged in ExecuteGA
        public static Ssbo CubiesBuffer, FreeMovesBuffer, SolvedBuffer, ActiveBuffer, PlanesBuffer, SeedMovesBuffer;
        static string Setup, Variables;
        static int CompiledN, CompiledSize;
        static uint NumSeeds;   // specimens pre-seeded by Init (host-built SeedMoves)
        public static int GenerationsCount;

        // --- TEMPORARY cost-breakdown profiling (A2 measure-first). Cross-cutting swEuler may overlap swExecGA
        // (if the seeder decomposes) and the CPU-orchestration remainder; report it as "how much time is Euler",
        // not a strict partition. Reset per batch, reported at batch end. Remove once we pick the A2 direction. ---
        public static Stopwatch swExecGA = new Stopwatch(), swSeed = new Stopwatch(), swCapture = new Stopwatch(),
                                 swEuler = new Stopwatch(), swScramble = new Stopwatch();
        public static void ProfReset() { swExecGA.Reset(); swSeed.Reset(); swCapture.Reset(); swEuler.Reset(); swScramble.Reset(); }
        public static string ProfReport(double totalMs)
        {
            double ga = swExecGA.Elapsed.TotalMilliseconds, seed = swSeed.Elapsed.TotalMilliseconds,
                   cap = swCapture.Elapsed.TotalMilliseconds, eul = swEuler.Elapsed.TotalMilliseconds,
                   scr = swScramble.Elapsed.TotalMilliseconds;
            string L(string name, double ms) => $"  {name,-16}{ms,9:F0} ms  {(totalMs > 0 ? 100 * ms / totalMs : 0),5:F1}%";
            return string.Join("\n", new[] {
                $"PROFILE  (batch total {totalMs:F0} ms)",
                L("ExecuteGA", ga),
                L("  - Seed", seed),
                L("  - Capture", cap),
                L("  - GPU rest", ga - seed - cap),
                L("Scramble set", scr),
                L("CPU orchestr.", totalMs - ga - scr),
                L("Euler (x-cut)", eul),
            });
        }

        // --- Instanced cube render (Gpu owns the GL resources; TGLContext gathers the scene). ---
        // All cubies share ONE base hypercube mesh (TCubie.Cube), differing only by their transform,
        // so we upload the mesh once and draw each cubie as an instance fed its ND world matrix.
        public static uint RenderProgram, CompositeProgram;
        static uint renderVs, renderFs, compositeVs, compositeFs, cubeVao, attrBuf, idxBuf;
        static uint diffuseTex, normalTex, arrowTex, roughTex, aoTex;   // sampled in CubeFragment (units 0..4)
        static uint oitFbo, opaqueTex, accumTex, revealTex, depthTex;   // WBOIT: opaque color + accum/revealage + shared depth
        static int oitW, oitH;                                          // current size of the OIT targets
        static int[] drawOrder;                                         // instance indices, opaque first then transparent
        static int cubeIndexCount;
        public static Ssbo PosBuffer, XformBuffer, LightsBuffer, MaterialsBuffer;   // bindings 8 / 9 / 10 / 11, after the GA's 0-7
        public static Ssbo ResultStateBuffer;   // binding 12: ReplayWinner output (winner's resulting cube state, OrthoPack per cubie)
        static float[] xformScratch;
        static int MaxLights;        // MAX_LIGHTS define; sizes the Lights UBO array and lightScratch
        static float[] lightScratch;


        // One-time GPU setup: reserve the buffers, create the program + shader objects (attached but
        // not yet compiled), then compile them for the current cube. Call once with a current GL context.
        public static void Init()
        {
            Variables = ReadManifestText("Resources.Variables.glsl.c");
            Variables = Variables.Replace("#define uint unsigned int", "");
            Setup = ReadManifestText("Resources.Setup.glsl.c");

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
            PosBuffer   = new Ssbo();                               // 8  base ND vertex positions (render)
            XformBuffer = new Ssbo();                               // 9  per-instance ND world matrices (render)
            LightsBuffer = new Ssbo(OpenGL.GL_UNIFORM_BUFFER);      // 10 std140 scene lights UBO (render)
            MaterialsBuffer = new Ssbo();                           // 11 per-face material (specular + shininess)
            ResultStateBuffer = new Ssbo();                         // 12 ReplayWinner output (created last -> binding 12)

            // Create the program + shader objects once and attach them. BuildShaders only (re)sources,
            // compiles and links these same objects, so nothing is ever created or deleted again.
            InitProgram = CreateComputeProgram();
            EvaluateMicroProgram = CreateComputeProgram();
            EvaluateMacroProgram = CreateComputeProgram();
            SortProgram = CreateComputeProgram();
            SelCrossoverProgram = CreateComputeProgram();
            ReplayWinnerProgram = CreateComputeProgram();

            // Render program is VS+FS (not compute), so create/attach both shaders here; BuildShaders
            // sources and compiles them (with N injected) alongside the compute programs.
            RenderProgram = CreateRenderProgram();

            // Composite program resolves the WBOIT targets; it is N-independent, so compile it once here
            // rather than in BuildShaders.
            CompositeProgram = CreateCompositeProgram();
            CompileCompositeProgram();

            // Material maps (independent of N/SIZE, so loaded once here). basecolor -> diffuse, normal
            // -> normal map (sampled through the vertex shader's TBN). roughness/AO/height come later.
            diffuseTex = LoadTexture("Resources.Wall_Stone_018_basecolor.jpg");
            normalTex  = LoadTexture("Resources.Wall_Stone_018_normal.png");
            arrowTex   = LoadTexture("Resources.Arrow.png");   // orientation decal (black arrow on white)
            roughTex   = LoadTexture("Resources.Wall_Stone_018_roughness.png");        // -> per-texel shininess
            aoTex      = LoadTexture("Resources.Wall_Stone_018_ambientOcclusion.jpg"); // -> ambient occlusion

            BuildShaders();
        }

        // Loads an embedded image into a mipmapped, repeat-wrapped RGBA8 texture and returns its id.
        // GDI bitmaps are top-down BGRA; flip vertically (GL's origin is bottom-left) and upload as BGRA.
        static uint LoadTexture(string dotPath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = $"{assembly.GetName().Name}.{dotPath}";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var bmp = new System.Drawing.Bitmap(stream);
            bmp.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);
            var rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            uint tex;
            OpenGL.GenTextures(1, &tex);
            OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, tex);
            OpenGL.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, (int)OpenGL.GL_RGBA8, bmp.Width, bmp.Height, 0,
                              OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, (void*)data.Scan0);
            bmp.UnlockBits(data);
            OpenGL.GenerateMipmap(OpenGL.GL_TEXTURE_2D);
            OpenGL.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, (int)OpenGL.GL_LINEAR_MIPMAP_LINEAR);
            OpenGL.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, (int)OpenGL.GL_LINEAR);
            OpenGL.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, (int)OpenGL.GL_REPEAT);
            OpenGL.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, (int)OpenGL.GL_REPEAT);
            return tex;
        }

        static int GetDefineValue(string defineName)
        {
            // Require a whitespace boundary after the name so "N" does not also match "N_SOMETHING".
            string targetToken = "#define " + defineName + " ";
            using var reader = new StringReader(Variables);
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

        // Raw string value of a #define, or null if there is no such define. Unlike GetDefineValue this does not
        // parse -- it serves floats/expressions and, crucially, reports ABSENCE, so a caller can log an
        // experiment's setting ONLY while its define exists and silently stop once it is removed (no compile-time
        // reference to a define that may be deleted).
        public static string TryGetDefine(string defineName)
        {
            string targetToken = "#define " + defineName + " ";
            using var reader = new StringReader(Variables);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.StartsWith(targetToken, StringComparison.Ordinal))
                    return line.Substring(targetToken.Length).Trim();
            }
            return null;
        }

        static void SetDefineValue(string defineName, int value)
        {
            var oldValue = GetDefineValue(defineName);
            if (oldValue == -1)   // no integer define by that name -> nothing to replace (no-op)
                return;
            var defineLine = "#define " + defineName + " ";
            Variables = Variables.Replace(defineLine + oldValue, defineLine + value);
        }

        // Endgame coherence LATCH. Coherence is a compile-time #if in the evaluator, but it must be a PHASE, not a
        // static flag: it STALLS the descent (rewards building structure over peeling down), yet is what closes the
        // endgame (build a coherent-4 gateway). So the host toggles it at runtime by rewriting the COHERENCE define
        // and recompiling ONLY the micro-evaluator (one cheap program). Solve() keeps it 0 through the descent and
        // latches it to 1 the first time the active cluster reaches the floor (scrambled <= N), then leaves it on so
        // the search may climb back above N to build the gateway. No-op when the value is unchanged (no recompile).
        public static void SetCoherence(int value)
        {
            if (GetDefineValue("COHERENCE") == value) return;
            SetDefineValue("COHERENCE", value);
            CompileComputeProgram(EvaluateMicroProgram, "Resources.EvaluateMicro.glsl.c");
            noOpPenalty = float.NaN;   // recompiled evaluator -> re-measure the no-op baseline on next ScoreCube
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
            MaxLights = GetDefineValue("MAX_LIGHTS");
            CompiledN = TAffine.N;
            CompiledSize = TRubikCube.Size;
            noOpPenalty = float.NaN;

            CompileComputeProgram(InitProgram, "Resources.Init.glsl.c");
            CompileComputeProgram(EvaluateMicroProgram, "Resources.EvaluateMicro.glsl.c");
            CompileComputeProgram(EvaluateMacroProgram, "Resources.EvaluateMacro.glsl.c");
            CompileComputeProgram(SortProgram, "Resources.Sort.glsl.c");
            CompileComputeProgram(SelCrossoverProgram, "Resources.SelCrossover.glsl.c");
            CompileComputeProgram(ReplayWinnerProgram, "Resources.ReplayWinner.glsl.c");

            // Render program (VS+FS) + base mesh - same rebuild path as the GA on N/SIZE change.
            CompileRenderProgram();
            BuildCubeMesh();
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
        static uint CreateComputeProgram()
        {
            var program = OpenGL.CreateProgram();
            var shader = OpenGL.CreateShader(OpenGL.GL_COMPUTE_SHADER);
            OpenGL.AttachShader(program, shader);
            return program;
        }
        static uint CreateRenderProgram()
        {
            var program = OpenGL.CreateProgram();
            renderVs = OpenGL.CreateShader(OpenGL.GL_VERTEX_SHADER);
            renderFs = OpenGL.CreateShader(OpenGL.GL_FRAGMENT_SHADER);
            OpenGL.AttachShader(program, renderVs);
            OpenGL.AttachShader(program, renderFs);
            uint rid;
            OpenGL.GenVertexArrays(1, &rid); cubeVao = rid;
            OpenGL.GenBuffers(1, &rid); attrBuf = rid;
            OpenGL.GenBuffers(1, &rid); idxBuf = rid;
            return program;
        }
        static uint CreateCompositeProgram()
        {
            var program = OpenGL.CreateProgram();
            compositeVs = OpenGL.CreateShader(OpenGL.GL_VERTEX_SHADER);
            compositeFs = OpenGL.CreateShader(OpenGL.GL_FRAGMENT_SHADER);
            OpenGL.AttachShader(program, compositeVs);
            OpenGL.AttachShader(program, compositeFs);
            return program;
        }
        // The composite shaders take no #include (no N, no Variables), so they are sourced verbatim.
        static void CompileCompositeProgram()
        {
            OpenGL.CompileShader(compositeVs, ReadManifestText("Resources.CompositeVertex.glsl.c"));
            OpenGL.CompileShader(compositeFs, ReadManifestText("Resources.CompositeFragment.glsl.c"));
            OpenGL.LinkProgram(CompositeProgram);
        }

        // (Re)source the program's single attached shader with the current Setup header, compile it and
        // relink. The objects already exist (created in Init); the shader handle is read back with
        // glGetAttachedShaders rather than cached, so the class keeps only the program handles.
        static void CompileComputeProgram(uint program, string dotPath)
        {
            uint shader;
            int count;
            OpenGL.GetAttachedShaders(program, 1, &count, &shader);
            var setup = Setup;
            setup = setup.Replace("#include \"Variables.glsl.c\"", Variables);
            var code = ReadManifestText(dotPath).Replace("#include \"Setup.glsl.c\"", setup);
            OpenGL.CompileShader(shader, code);
            OpenGL.LinkProgram(program);
        }

        // Size of one Specimen in ints - the SINGLE place that knows the layout, which must match
        // `struct Specimen` in Setup.glsl.c: Fitness, MovesCount, Moves[GENES_COUNT], Structure.
        static int SpecimenInts => TChromosome.GenesLength + 3;

        // Uploads the per-run data into the buffers reserved in Init (0 Population, 1 NewPopulation,
        // 2 Cubies, 3 FreeMoves, 4 SolvedCubies, 5 ActiveCubies, 6 Planes UBO). Update reuses the same
        // ids/bindings and resizes the store as needed, so there is no buffer churn between GA runs.
        public static void CreateBuffers(TRubikCube cube)
        {
            // Keep the shader in sync with the cube: N/SIZE are compile-time defines, so recompile
            // when the dimension or size changed, otherwise the shader reads Cubies out of bounds.
            if (CompiledN != TAffine.N || CompiledSize != TRubikCube.Size)
                BuildShaders();         
            var populationSize = TGA<TRubikGenome>.PopulationCount * SpecimenInts * sizeof(int);
            var cubies = cube.PackCubies();
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
            fixed (int* p = solved) SolvedBuffer.Update(solved.Length * sizeof(int), p);
            fixed (int* p = active) ActiveBuffer.Update(active.Length * sizeof(int), p);
            fixed (int* p = planes) PlanesBuffer.Update(planes.Length * sizeof(int), p);
            ResultStateBuffer.Update(cube.Cubies.Length * sizeof(uint), null);

            if (cube.ActiveCubie != null)
            {
                var moves = cube.FreeMoves.ToArray();
                fixed (int* p = moves) FreeMovesBuffer.Update(moves.Length * sizeof(int), p);
                // Per-specimen seed sequences for Init: reverse-transform moves that undo active-cluster
                // cubies. SEED_RATIO % of the population is seeded (rest random); stride = plane count P =
                // N*(N-1)/2 genes, matching SEED_STRIDE (mixed-Givens seeds can be longer than N-1 moves).
                swSeed.Start();
                var seedMoves = cube.BuildSeedMoves(TGA<TRubikGenome>.PopulationCount, TAffine.Planes.Length,
                                                    GetDefineValue("SEED_RATIO"), GetDefineValue("SEED_MODE") != 0,
                                                    SeedFast, out NumSeeds);
                swSeed.Stop();
                fixed (int* p = seedMoves) SeedMovesBuffer.Update(seedMoves.Length * sizeof(int), p);
            }
        }

        // On-demand seed-pool telemetry (File menu). Builds the pool TWICE on the SAME cubie -- once fast, once
        // backtracking -- and returns both reports. Running both here is the whole point: SEED_FAST is read from
        // the Variables text loaded at Init, so flipping it otherwise needs an edit + restart, by which time the
        // cube is in a different state and the two pools are not comparable (we already got burned comparing a
        // depth-4 cubie against a depth-2 one). Same coordinates, same orientation, two pools side by side.
        // The production run uses whichever SeedFast selects; this is diagnostic only -- it operates on cubie
        // copies, mutates nothing, and touches no GPU buffer.
        public static string SeedStats(TRubikCube cube)
        {
            if (cube.ActiveCubie == null)
                return "No active cubie - select a scrambled cluster first (a solved cube has nothing to seed).";
            int ratio = GetDefineValue("SEED_RATIO");
            bool mixed = GetDefineValue("SEED_MODE") != 0;
            var sb = new System.Text.StringBuilder();
            TRubikCube.Diagnostic = true;                        // enable the census + report for these calls only
            foreach (var fast in new[] { true, false })
            {
                cube.BuildSeedMoves(TGA<TRubikGenome>.PopulationCount, TAffine.Planes.Length, ratio, mixed, fast, out _);
                sb.AppendLine($"===== SEED_FAST = {(fast ? 1 : 0)}{(fast == SeedFast ? "   <-- production uses this now" : "")} =====");
                sb.AppendLine(cube.LastSeedReport);
            }
            TRubikCube.Diagnostic = false;
            return sb.ToString();
        }

        // Scores the current cube on the GPU with the SAME evaluator the GA uses: a zero specimen (all
        // genes 0 = 0-degree identity moves after the quarter-turn encoding) leaves the cube unchanged,
        // so the evaluator returns baseline + no-op penalty. We subtract the penalty - MEASURED, not
        // hardcoded, as the same zero specimen's fitness on a SOLVED cube (baseline == 0, so the fitness
        // is exactly the penalty). One scorer (the evaluator), no separate kernel, no magic constant,
        // and the baseline tracks any future change to the no-op penalty automatically.
        // Coherence LATCH THRESHOLD + endgame NORMALIZER base. Single source of truth for both roles: the host
        // reads it to flip Coherent 0->1 when the residual reaches it (UpdateCoherenceLatch), and it is uploaded
        // to uniform 7 so the factor can normalize by FLOOR * N (pinning the scattered floor state to fitness
        // 1.0). It no longer WALKS -- fixed at FloorStart, reset per cluster (ResetFloor). Both the GA and
        // EvalZeroSpecimen upload THIS value, or Fitness and Score are incomparable.
        public static uint Floor = 4;

        // Coherence LATCH (uniform 8): 0 = bare count-peel (descent), 1 = endgame coherence discount. The host
        // sets it to 1 once the active cluster's residual reaches Floor and back to 0 for each new cluster. Like
        // Floor, BOTH the GA and EvalZeroSpecimen must use the same value or Fitness and Score are incomparable.
        public static uint Coherent = 0;

        // Seeding follows the coherence LATCH, because the two phases consume DIFFERENT manoeuvre material.
        // While peeling (Coherent = 0) the FAST path fits: a single greedy descent dead-ends more often the
        // LONGER the path, so it filters long manoeuvres out and the pool comes out weighted toward short ones
        // (measured on a deep 2^5 cubie: 41% one-move, 71% <= two moves) -- exactly what "solve one cubie
        // without disturbing the rest" needs. Once coherence latches on, the endgame needle needs the opposite:
        // the backtracking pool rescues the long macro/commutator sequences the fast path throws away, and its
        // dedup re-weights the pool toward them (distinct short solutions are few, distinct long ones many).
        // SEED_FAST still gates the descent half, so setting it to 0 means "backtracking in BOTH phases".
        static bool SeedFast => GetDefineValue("SEED_FAST") != 0 && Coherent == 0;

        static float noOpPenalty;   // reset to NaN in BuildShaders (Init + on N/SIZE change) -> measured on first ScoreCube
        public static float ScoreCube(TRubikCube cube)
        {
            var score = EvalZeroSpecimen(cube);
            if (float.IsNaN(noOpPenalty))
                noOpPenalty = EvalZeroSpecimen(new TRubikCube());
            return score - noOpPenalty;
        }

        // Evaluates ONE all-zero specimen (identity
        // moves) and returns its fitness = the cube's raw score PLUS the no-op penalty (a zero specimen
        // never changes the active cluster, so the penalty always fires).
        static float EvalZeroSpecimen(TRubikCube cube)
        {
            CreateBuffers(cube);
            var micro = cube.Cubies.Length <= 82;   // Micro carries the coherence eval; 82 admits 3^4 (81) and 4^3 (64)
            var eval = micro ? EvaluateMicroProgram : EvaluateMacroProgram;
            var spec = new int[SpecimenInts];                  // all zero = identity moves
            fixed (int* p = spec) Population.Update(spec.Length * sizeof(int), p);
            OpenGL.UseProgram(eval);
            OpenGL.Uniform1ui(4, (uint)cube.SolvedCubies.Count);
            OpenGL.Uniform1ui(5, (uint)cube.ActiveCluster.Count);
            OpenGL.Uniform1ui(7, Floor);       // latch threshold + endgame normalizer base -- MUST match the GA's
            OpenGL.Uniform1ui(8, Coherent);    // MUST match the GA's latch, or Score and Fitness differ
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
            var micro = cube.Cubies.Length <= 82;   // Micro carries the coherence eval; 82 admits 3^4 (81) and 4^3 (64)
            var evalProgram = micro ? EvaluateMicroProgram : EvaluateMacroProgram;

            // Create + upload all buffers for this run (population, packed cube, free moves, indices, planes).
            CreateBuffers(cube);
            var solvedCount = (uint)cube.SolvedCubies.Count;
            var activeCount = (uint)cube.ActiveCluster.Count;

            // Dispatch dims. Init/SelCrossover: 1 thread per specimen (blocks of 64).
            // Evaluate: micro = 1 thread/specimen; macro = 1 block (256 threads) per specimen.
            uint genGroups = (populationCount + 63) / 64;
            uint evalGroups = micro ? genGroups : populationCount;

            int parentSlot = 0;
            var best = new TRubikGenome();
            var specimenSize = SpecimenInts * sizeof(int);
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
                    OpenGL.Uniform1ui(7, Floor);
                    OpenGL.Uniform1ui(8, Coherent);
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
                        swCapture.Start();
                        CaptureWinnerState(parent, best, cube);
                        swCapture.Stop();
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
            }
            return best;
        }

        // Replays the winning specimen (Population[0], just sorted to the front) on the GPU and reads back the
        // RESULTING cube state (OrthoPack per cubie) into best.ResultPacked -- the host then ingests the state the
        // GA already computed instead of replaying moves (TAffine turns) and decomposing orientation (Euler). Reads
        // the winner IN PLACE, before selection/crossover overwrites Population[0]. Binds Population->0, Cubies->2,
        // ResultState->12; the loop rebinds 0/1 for crossover right after, so this leaves nothing stale.
        static void CaptureWinnerState(Ssbo parent, TRubikGenome best, TRubikCube cube)
        {
            int n = cube.Cubies.Length;
            OpenGL.UseProgram(ReplayWinnerProgram);
            OpenGL.BindBufferBase(parent.Type, 0, parent.Id);
            CubiesBuffer.Bind();
            ResultStateBuffer.Bind();
            OpenGL.DispatchCompute((uint)((n + 63) / 64), 1, 1);
            OpenGL.MemoryBarrier(OpenGL.MemoryBarrierFlags.ShaderStorage);
            if (best.ResultPacked == null || best.ResultPacked.Length != n)
                best.ResultPacked = new uint[n];
            fixed (uint* p = best.ResultPacked) ResultStateBuffer.GetBufferData(p);
        }

        // Compiles the render program (VS+FS): both pull in the Variables block directly (VS needs N, the
        // FS needs MAX_LIGHTS for the Lights UBO), not the Setup header the compute shaders use.
        static void CompileRenderProgram()
        {
            var dotPath = "Resources.CubeVertex.glsl.c";
            var code = ReadManifestText(dotPath).Replace("#include \"Variables.glsl.c\"", Variables);
            OpenGL.CompileShader(renderVs, code);
            dotPath = "Resources.CubeFragment.glsl.c";
            code = ReadManifestText(dotPath).Replace("#include \"Variables.glsl.c\"", Variables);
            OpenGL.CompileShader(renderFs, code);
            OpenGL.LinkProgram(RenderProgram);
        }

        // (Re)builds the shared base hypercube mesh into the render buffers from TCubie.Cube: base ND
        // positions -> PosBuffer (vertex-pulled), interleaved uv+color -> VAO, triangulated quads -> EBO.
        static void BuildCubeMesh()
        {
            TCubie.Cube = TShape.CreateHyperCube();
            var mesh = TCubie.Cube;
            int n = TAffine.N;
            int faceCount = mesh.Materials.Count;      // one material per quad face
            int vertCount = 4 * faceCount;          // quads expanded per-face (uv/color differ per face)
            cubeIndexCount = 6 * faceCount;         // two triangles per quad

            var pos = new float[n * vertCount];
            var attr = new byte[12 * vertCount];    // interleaved: uv (2 float) + color (4 byte)
            var idx = new uint[cubeIndexCount];
            fixed (byte* pa = attr)
            {
                for (int f = 0; f < faceCount; f++)
                {
                    var col = mesh.Materials[f].Diffuse.Color;
                    for (int k = 0; k < 4; k++)
                    {
                        int v = 4 * f + k;
                        var corner = mesh.Vertices[mesh.Faces[4 * f + k]];
                        for (int d = 0; d < n; d++)
                            pos[n * v + d] = corner.Data[d];
                        var uv = mesh.UV[v];                    // per-vertex uv authored in CreateHyperCube
                        float* fu = (float*)(pa + 12 * v);
                        fu[0] = uv.Data[0];
                        fu[1] = uv.Data[1];
                        byte* bc = pa + 12 * v + 8;
                        bc[0] = col.R; bc[1] = col.G; bc[2] = col.B; bc[3] = 255;
                    }
                    int b = 4 * f, o = 6 * f;
                    idx[o] = (uint)b; idx[o + 1] = (uint)(b + 1); idx[o + 2] = (uint)(b + 2);
                    idx[o + 3] = (uint)b; idx[o + 4] = (uint)(b + 2); idx[o + 5] = (uint)(b + 3);
                }
            }

            OpenGL.BindVertexArray(cubeVao);
            OpenGL.BindBuffer(OpenGL.GL_ARRAY_BUFFER, attrBuf);
            fixed (byte* pa = attr)
                OpenGL.BufferData(OpenGL.GL_ARRAY_BUFFER, attr.Length, pa, OpenGL.GL_STATIC_DRAW);
            OpenGL.VertexAttribPointer(0, 2, OpenGL.GL_FLOAT, (byte)0, 12, (void*)0);           // uv
            OpenGL.EnableVertexAttribArray(0);
            OpenGL.VertexAttribPointer(1, 4, OpenGL.GL_UNSIGNED_BYTE, (byte)1, 12, (void*)8);   // color (normalized)
            OpenGL.EnableVertexAttribArray(1);
            OpenGL.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, idxBuf);
            fixed (uint* pi = idx)
                OpenGL.BufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER, idx.Length * sizeof(uint), pi, OpenGL.GL_STATIC_DRAW);
            OpenGL.BindVertexArray(0);

            fixed (float* pp = pos) PosBuffer.Update(pos.Length * sizeof(float), pp);

            // Per-face material record (one vec4 per base face): rgb = specular color, w = shininess.
            // The vertex shader pulls it by face index (gl_VertexID / 4) and forwards it flat.
            var matData = new float[faceCount * 4];
            for (int f = 0; f < faceCount; f++)
            {
                var m = mesh.Materials[f];
                matData[4 * f]     = m.Specular.Color.R / 255f;
                matData[4 * f + 1] = m.Specular.Color.G / 255f;
                matData[4 * f + 2] = m.Specular.Color.B / 255f;
                matData[4 * f + 3] = m.Shininess;
            }
            fixed (float* pm = matData) MaterialsBuffer.Update(matData.Length * sizeof(float), pm);
        }

        // Accumulated world transform of a scene node, walking parent -> ... -> root (same order as
        // TGLContext.GatherInstances: world = root.Transform * ... * node.Transform). Lights carry no
        // faces, so they are not gathered there; this recovers a light's world position on demand.
        static TAffine WorldOf(TShape node)
        {
            var world = node.Transform;
            for (var p = node.Parent; p != null; p = p.Parent)
                world = p.Transform * world;
            return world;
        }

        static void Put3(float[] buf, int at, TVector v)   // copy up to 3 components, zero-pad the rest
        {
            for (int d = 0; d < 3; d++)
                buf[at + d] = d < v.Size ? v.Data[d] : 0f;
        }

        // Packs the enabled scene lights into the std140 Lights UBO (binding 10): a vec4 header (x = count)
        // followed by MAX_LIGHTS records of 5 vec4 (position+flag, ambient, diffuse, specular, attenuation).
        // Each light's ND world position is projected to 3D the same way the mesh is (first up-to-3 coords).
        // Call each frame before DrawCube - light/root transforms change as the scene is rotated.
        public static void UpdateLights(List<TLight> lights)
        {
            const int rec = 20;                            // floats per light record (5 * vec4)
            int total = 4 + MaxLights * rec;               // vec4 header + MAX_LIGHTS records
            if (lightScratch == null || lightScratch.Length != total)
                lightScratch = new float[total];
            Array.Clear(lightScratch, 0, lightScratch.Length);

            int n = 0;
            for (int i = 0; i < lights.Count && n < MaxLights; i++)
            {
                var lgt = lights[i];
                if (!lgt.IsEnabled) continue;
                int b = 4 + n * rec;
                Put3(lightScratch, b, WorldOf(lgt).Origin);            // world position (or direction)
                lightScratch[b + 3] = lgt.IsDirectional ? 1f : 0f;    // Position.w = isDirectional
                Put3(lightScratch, b + 4, lgt.Ambient);
                Put3(lightScratch, b + 8, lgt.Diffuse);
                Put3(lightScratch, b + 12, lgt.Specular);
                Put3(lightScratch, b + 16, lgt.AttCoeff);
                n++;
            }
            lightScratch[0] = n;                           // header: active light count
            fixed (float* p = lightScratch) LightsBuffer.Update(total * sizeof(float), p);
        }

        static void BindMaterialTextures()   // units 0..4, matching the sampler(binding=) qualifiers in CubeFragment
        {
            OpenGL.ActiveTexture(OpenGL.GL_TEXTURE0);      OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, diffuseTex);
            OpenGL.ActiveTexture(OpenGL.GL_TEXTURE0 + 1);  OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, normalTex);
            OpenGL.ActiveTexture(OpenGL.GL_TEXTURE0 + 2);  OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, arrowTex);
            OpenGL.ActiveTexture(OpenGL.GL_TEXTURE0 + 3);  OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, roughTex);
            OpenGL.ActiveTexture(OpenGL.GL_TEXTURE0 + 4);  OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, aoTex);
        }

        static void AllocTex(uint tex, int internalFmt, uint fmt, uint type, int w, int h)
        {
            OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, tex);
            OpenGL.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, internalFmt, w, h, 0, fmt, type, null);
            OpenGL.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, (int)OpenGL.GL_NEAREST);
            OpenGL.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, (int)OpenGL.GL_NEAREST);
            OpenGL.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, (int)OpenGL.GL_CLAMP_TO_EDGE);
            OpenGL.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, (int)OpenGL.GL_CLAMP_TO_EDGE);
        }

        // (Re)allocates the WBOIT framebuffer + its attachments to the viewport size: opaque color (RGBA8),
        // accum (RGBA16F), revealage (R16F) and a shared depth texture. Only rebuilt when the size changes.
        static void EnsureOitTargets(int w, int h)
        {
            if (oitFbo != 0 && w == oitW && h == oitH) return;
            oitW = w; oitH = h;
            if (oitFbo == 0)
            {
                uint id;
                OpenGL.GenFramebuffers(1, &id); oitFbo = id;
                OpenGL.GenTextures(1, &id); opaqueTex = id;
                OpenGL.GenTextures(1, &id); accumTex = id;
                OpenGL.GenTextures(1, &id); revealTex = id;
                OpenGL.GenTextures(1, &id); depthTex = id;
            }
            AllocTex(opaqueTex, (int)OpenGL.GL_RGBA8,              OpenGL.GL_RGBA,            OpenGL.GL_UNSIGNED_BYTE, w, h);
            AllocTex(accumTex,  (int)OpenGL.GL_RGBA16F,           OpenGL.GL_RGBA,            OpenGL.GL_FLOAT,         w, h);
            AllocTex(revealTex, (int)OpenGL.GL_R16F,              OpenGL.GL_RED,             OpenGL.GL_FLOAT,         w, h);
            AllocTex(depthTex,  (int)OpenGL.GL_DEPTH_COMPONENT24, OpenGL.GL_DEPTH_COMPONENT, OpenGL.GL_FLOAT,         w, h);
            OpenGL.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, oitFbo);
            OpenGL.FramebufferTexture2D(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT0, OpenGL.GL_TEXTURE_2D, opaqueTex, 0);
            OpenGL.FramebufferTexture2D(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT1, OpenGL.GL_TEXTURE_2D, accumTex,  0);
            OpenGL.FramebufferTexture2D(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT2, OpenGL.GL_TEXTURE_2D, revealTex, 0);
            OpenGL.FramebufferTexture2D(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_DEPTH_ATTACHMENT,  OpenGL.GL_TEXTURE_2D, depthTex,  0);
            OpenGL.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);
        }

        // Renders the gathered scene with weighted-blended OIT. Instances are packed opaque-first then
        // transparent; M (column-major) + Origin + per-instance alpha go into XformBuffer. Three passes:
        //   1. opaque (alpha == 1)  -> opaque color + depth  (depth write on, no blend)
        //   2. transparent (alpha < 1) -> accum + revealage against that depth (depth test, no write)
        //   3. composite the two into the default framebuffer. Rebuilds programs+mesh on N/SIZE change.
        public static void RenderScene(List<TAffine> instances, List<float> alphas, System.Drawing.Color bg, int w, int h)
        {
            if (w <= 0 || h <= 0) return;
            if (CompiledN != TAffine.N || CompiledSize != TRubikCube.Size)
                BuildShaders();

            int n = TAffine.N, total = instances.Count, stride = n * n + n + 1;   // + 1 alpha slot

            // Partition instance indices: opaque first, transparent after (one instanced draw per slice).
            if (drawOrder == null || drawOrder.Length < total) drawOrder = new int[total];
            int nOpaque = 0;
            for (int i = 0; i < total; i++) if (alphas[i] >= 1f) drawOrder[nOpaque++] = i;
            int k = nOpaque;
            for (int i = 0; i < total; i++) if (alphas[i] < 1f) drawOrder[k++] = i;
            int nTrans = total - nOpaque;

            if (xformScratch == null || xformScratch.Length < stride * total)
                xformScratch = new float[stride * total];
            for (int j = 0; j < total; j++)
            {
                int i = drawOrder[j];
                Array.Copy(instances[i].M.Data, 0, xformScratch, j * stride, n * n);            // M (column-major)
                Array.Copy(instances[i].Origin.Data, 0, xformScratch, j * stride + n * n, n);    // Origin
                xformScratch[j * stride + n * n + n] = alphas[i];                                // alpha
            }
            fixed (float* p = xformScratch) XformBuffer.Update(stride * total * sizeof(float), p);

            EnsureOitTargets(w, h);
            OpenGL.Viewport(0, 0, w, h);
            OpenGL.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, oitFbo);

            OpenGL.UseProgram(RenderProgram);
            PosBuffer.Bind(); XformBuffer.Bind(); LightsBuffer.Bind(); MaterialsBuffer.Bind();   // bindings 8/9/10/11
            BindMaterialTextures();
            OpenGL.BindVertexArray(cubeVao);

            // 1. Opaque pass -> color attachment 0 (+ shared depth).
            uint att0 = OpenGL.GL_COLOR_ATTACHMENT0;
            OpenGL.DrawBuffers(1, &att0);
            OpenGL.Enable(OpenGL.GL_DEPTH_TEST);
            OpenGL.DepthMask((byte)1);
            OpenGL.Disable(OpenGL.GL_BLEND);
            OpenGL.ClearColor(bg.R / 255f, bg.G / 255f, bg.B / 255f, 1f);
            OpenGL.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
            OpenGL.Uniform1i(1, 0);           // uOIT = 0 (opaque)
            OpenGL.Uniform1i(0, 0);           // uInstanceBase = 0
            if (nOpaque > 0)
                OpenGL.DrawElementsInstanced(OpenGL.GL_TRIANGLES, cubeIndexCount, OpenGL.GL_UNSIGNED_INT, (void*)0, nOpaque);

            // 2. Transparent pass -> accum (attachment 1) + revealage (attachment 2), depth test, no write.
            uint* atts = stackalloc uint[2] { OpenGL.GL_COLOR_ATTACHMENT1, OpenGL.GL_COLOR_ATTACHMENT2 };
            OpenGL.DrawBuffers(2, atts);
            float* zero = stackalloc float[4] { 0f, 0f, 0f, 0f };
            float* one  = stackalloc float[4] { 1f, 1f, 1f, 1f };
            OpenGL.ClearBufferfv(OpenGL.GL_COLOR, 0, zero);   // accum = 0
            OpenGL.ClearBufferfv(OpenGL.GL_COLOR, 1, one);    // revealage = 1
            OpenGL.DepthMask((byte)0);
            OpenGL.Enable(OpenGL.GL_BLEND);
            OpenGL.BlendFunci(0, OpenGL.GL_ONE, OpenGL.GL_ONE);                    // accum: additive
            OpenGL.BlendFunci(1, OpenGL.GL_ZERO, OpenGL.GL_ONE_MINUS_SRC_COLOR);   // reveal: *= (1 - a)
            OpenGL.Uniform1i(1, 1);           // uOIT = 1 (transparent)
            OpenGL.Uniform1i(0, nOpaque);     // uInstanceBase = nOpaque
            if (nTrans > 0)
                OpenGL.DrawElementsInstanced(OpenGL.GL_TRIANGLES, cubeIndexCount, OpenGL.GL_UNSIGNED_INT, (void*)0, nTrans);
            OpenGL.Disable(OpenGL.GL_BLEND);
            OpenGL.DepthMask((byte)1);

            // 3. Composite the OIT targets over the opaque color into the default framebuffer.
            OpenGL.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);
            OpenGL.Disable(OpenGL.GL_DEPTH_TEST);
            OpenGL.UseProgram(CompositeProgram);
            OpenGL.ActiveTexture(OpenGL.GL_TEXTURE0);      OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, opaqueTex);
            OpenGL.ActiveTexture(OpenGL.GL_TEXTURE0 + 1);  OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, accumTex);
            OpenGL.ActiveTexture(OpenGL.GL_TEXTURE0 + 2);  OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, revealTex);
            OpenGL.BindVertexArray(cubeVao);   // a VAO must be bound to draw in a core profile
            OpenGL.DrawArrays(OpenGL.GL_TRIANGLES, 0, 3);
            OpenGL.BindVertexArray(0);
            OpenGL.Enable(OpenGL.GL_DEPTH_TEST);   // restore for the next frame
        }
    }
}
