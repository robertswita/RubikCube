using GA;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using TGL;

namespace RubikCube
{
    public class TRubikGenome: TChromosome
    {
        public int StartIndex;
        public int StartPos;
        public int StopPos;
        public int MoveCount;
        public static List<int> FreeMoves;
        public static TRubikCube RubikCube;

        //public TRubikGenome(): base()
        //{
        //    Correct();
        //    //var geneIdx = Rnd.Next(Genes.Length - 3);
        //    //Commute(geneIdx);
        //    //Mutate();
        //}

        public override TChromosome Clone()
        {
            var clone = (TRubikGenome)base.Clone();
            clone.MoveCount = MoveCount;
            return clone;
        }
        public override void MutateGene(int idx)
        {
            Genes[idx] = FreeMoves[Rnd.Next(FreeMoves.Count)];
            //if (idx == GenesLength - 1)
            //{
            //    //Check();
            //    //Mutate();
            //}
        }

        public void Conjugate()
        {
            //Check();
            //var geneIdx = Rnd.Next(Genes.Length / 2);
            //for (int i = 1; i <= geneIdx; i++)
            //{
            //    var move = TMove.Decode((int)Genes[geneIdx - i]);
            //    move.Angle = 2 - move.Angle;
            //    Genes[geneIdx + i] = move.Encode();
            //}
            //var lastMove = TMove.Decode((int)Genes[geneIdx]);
            //lastMove.Angle = 2 - lastMove.Angle;
            //Genes[2 * geneIdx + 1] = lastMove.Encode();


            //var geneIdx = Rnd.Next(Genes.Length - 4);
            //for (int i = 0; i < 2; i++)
            //{
            //    var move = TMove.Decode((int)Genes[geneIdx + i]);
            //    move.Angle = 2 - move.Angle;
            //    Genes[geneIdx + 4 - i] = move.Encode();
            //}

            //var startPos = Rnd.Next((Genes.Length - TAffine.N + 1) / 2);
            var startPos = Rnd.Next(Genes.Length / 2);
            var stopPos = startPos + 1 + TChromosome.Rnd.Next(TAffine.N - 1);// TChromosome.Rnd.Next(Genes.Length / 2 - startPos);// 1;// TAffine.N - 1;
            if (stopPos > Genes.Length / 2) stopPos = Genes.Length / 2;

            var hist = new int[TMove.SizeMatrix.Data.Length / 3];
            for (int idx = startPos; idx < stopPos; idx++)
            {
                var code = (int)Genes[idx];
                hist[code / 3] += code % 3 + 1;
            }

            //var middleGene = (int)Genes[startPos];
            //var middleMovesCount = 0;
            //var doubleAngle = middleGene % 3 == 1;
            var pos = stopPos;
            for (int i = startPos - 1; i >= 0; i--)
            {
                var gene = (int)Genes[i];
                //if (!doubleAngle || Math.Abs(gene - middleGene) > 1)
                //{
                //    gene = TMove.GetRevCode(gene);
                //    middleMovesCount++;
                //}
                if ((hist[gene / 3] & 3) != 2)
                    gene = TMove.GetRevCode(gene);
                Genes[pos++] = gene;
            }
            //if (doubleAngle && (middleMovesCount & 1) == 0)
            //Genes[pos++] = middleGene;
            for (int i = stopPos - 1; i >= startPos; i--)
                Genes[pos++] = TMove.GetRevCode((int)Genes[i]);
            //MoveCount = pos;
            //Macromize();
        }

        public void Commute(int geneIdx)
        {
            //Check();
            //var geneIdx = Rnd.Next(Genes.Length - 3);
            //for (int i = 0; i < 2; i++)
            //{
            //    var move = TMove.Decode((int)Genes[geneIdx + i]);
            //    move.Angle = 2 - move.Angle;
            //    Genes[geneIdx + 2 + i] = move.Encode();
            //}
            for (int i = 0; i <= geneIdx; i++)
            {
                var move = TMove.Decode((int)Genes[i]);
                move.Angle = 2 - move.Angle;
                Genes[geneIdx + 1 + i] = move.Encode();
            }
        }

        public override void MutateAux()
        {
            var geneIdx = Rnd.Next(Genes.Length / 2);
            MutateGene(geneIdx);
        }

        public static void NextGeneration()
        {
            //if (!TRubikCube.IsEulerOrderReversed)
            //    TRubikCube.EulerOrder = RubikCube.GetOrder();
            //TRubikCube.IsEulerOrderReversed = !TRubikCube.IsEulerOrderReversed;
        }

        public override void Mutate()
        {
            //var geneIdx = Rnd.Next(Genes.Length / 2);
            //MutateGene(geneIdx);

            //Conjugate();
            //Commute(geneIdx);
            //var flipCoin = Rnd.Next(2);
            //if (flipCoin > 0)
            //    Commute(geneIdx);
            //else
            //    Conjugate(geneIdx);
            //var coinToss = Rnd.Next(2);
            //var seq = coinToss != 0 ? RubikCube.Seq : RubikCube.RevSeq;
            //var seq = RubikCube.ActiveCubie.IsReversedSeq ? RubikCube.RevSeq : RubikCube.ActSeq;
            //RubikCube.ActiveCubie.IsReversedSeq = !RubikCube.ActiveCubie.IsReversedSeq;
            var seq = RubikCube.ActSeq;
            //if (!TRubikCube.IsEulerOrderReversed)
            //    TRubikCube.EulerOrder = RubikCube.GetOrder();
            //TRubikCube.IsEulerOrderReversed = !TRubikCube.IsEulerOrderReversed;

            //var seq = RubikCube.GetReversedSeq();
            for (int i = 0; i < seq.Count; i++)
            {
                Genes[i] = seq[i];
                //if (!FreeMoves.Contains((int)Genes[i]))
                //    ;
            }
            ////var move = TMove.Decode((int)Genes[seq.Count]);
            ////move.Slice = (int)Math.Round(RubikCube.ActivePos[move.Axis] + TRubikCube.C);
            ////Genes[seq.Count] = move.Encode();

            Conjugate();
            //var startPos = seq.Count;
            //var stopPos = startPos + 1 + TChromosome.Rnd.Next(TAffine.N);// TChromosome.Rnd.Next(Genes.Length / 2 - startPos);// 1;// TAffine.N - 1;
            //if (stopPos > Genes.Length / 2) stopPos = Genes.Length / 2;
            //var pos = stopPos;
            //for (int i = startPos - 1; i >= 0; i--)
            //    Genes[pos++] = TMove.GetRevCode((int)Genes[i]);
            //for (int i = stopPos - 1; i >= startPos; i--)
            //    Genes[pos++] = TMove.GetRevCode((int)Genes[i]);
            //for (int i = 0; i < seq.Count; i++)
            //    Genes[i] = seq[i];
            //Conjugate();


            //var seq = RubikCube.ActSeq;
            //for (int i = 0; i < seq.Count; i++)
            //{
            //    Genes[i] = seq[i];
            //    //if (!FreeMoves.Contains((int)Genes[i]))
            //    //    ;
            //}
            //var setupMoves = 1 + Rnd.Next(10);
            ////MutateGene(seq.Count / 2);
            //for (int i = 0; i < setupMoves; i++)
            //{
            //    var rndGene = FreeMoves[Rnd.Next(FreeMoves.Count)];
            //    Genes[seq.Count + i] = rndGene;
            //    var rndMove = TMove.Decode(rndGene);
            //    rndMove.Angle = 2 - rndMove.Angle;
            //    Genes[2 * (seq.Count + setupMoves) - 1 - i] = rndMove.Encode();
            //}
            //for (int i = seq.Count - 1; i >= 0; i--)
            //{
            //    var move = TMove.Decode(seq[i]);
            //    move.Angle = 2 - move.Angle;
            //    Genes[setupMoves + 2 * seq.Count - 1 - i] = move.Encode();
            //}

        }

        public override TChromosome Crossover(TChromosome other, int splitIdx)
        {
            var child = new TRubikGenome();
            splitIdx /= 2;
            //splitIdx++;

            var startPos = splitIdx;
            var stopPos = startPos + 1 + TChromosome.Rnd.Next(TAffine.N - 1);
            if (stopPos > Genes.Length / 2) stopPos = Genes.Length / 2;
            //var stopPos = startPos + TChromosome.Rnd.Next(Genes.Length / 2 - startPos);
            child.StartPos = startPos;
            child.StopPos = stopPos;
            Array.Copy(Genes, 0, child.Genes, 0, Genes.Length);
            //var seq = RubikCube.ActSeq;
            //for (int i = 0; i < seq.Count; i++)
            //    child.Genes[i] = seq[i];
            Array.Copy(other.Genes, startPos, child.Genes, startPos, stopPos - startPos);
            var pos = stopPos;
            for (int i = startPos - 1; i >= 0; i--)
                child.Genes[pos++] = TMove.GetRevCode((int)child.Genes[i]);
            for (int i = stopPos - 1; i >= startPos; i--)
                child.Genes[pos++] = TMove.GetRevCode((int)child.Genes[i]);

            //child.Conjugate();
            //child.Conjugate();
            //var move = TMove.Decode((int)Genes[seq.Count]);
            //move.Slice = (int)Math.Round(RubikCube.ActivePos[move.Axis] + TRubikCube.C);
            //Genes[seq.Count] = move.Encode();
            return child;
        }

        public void Macromize()
        {
            var hist = new int[TMove.SizeMatrix.Data.Length / 3];
            for (int idx = 0; idx < MoveCount; idx++)
            {
                var code = (int)Genes[idx];
                hist[code / 3] += code % 3 + 1;
            }
            for (int i = 0; i < hist.Length; i++)
            {
                var angle = hist[i] & 3;
                if (angle > 0 && MoveCount < Genes.Length)
                    Genes[MoveCount++] = 3 * i + 3 - angle;
            }
        }

        //bool IsChecked;
        public override void Correct()
        {
            //var fitness = Evaluate();
            //var oldGenes = (double[])Genes.Clone();
            //if (IsChecked) return;
            for (int idx = StartIndex; idx < MoveCount; idx++)
            {
                var code = (int)Genes[idx];
                var move = TMove.Decode(code);
                for (int prevIdx = idx - 1; prevIdx >= StartIndex; prevIdx--)
                {
                    var prevMove = TMove.Decode((int)Genes[prevIdx]);
                    if (prevMove.Axis != move.Axis) break;
                    if (prevMove.Plane != move.Plane) break;
                    if (prevMove.Slice == move.Slice)
                    {
                        var angle = ((move.Angle + prevMove.Angle + 2) & 3) - 1;
                        if (angle >= 0)
                        {
                            prevMove.Angle = angle;
                            Genes[prevIdx] = prevMove.Encode();
                        }
                        else
                        {
                            RemoveGene(prevIdx);
                            idx--;
                        }
                        RemoveGene(idx);
                        idx--;
                        break;
                    }
                }
            }
            //var newFit = Evaluate();
            //if (fitness < newFit && MoveCount > 0)
            //{
            //    var oldSpec = new TRubikGenome();
            //    oldSpec.Genes = oldGenes;
            //    fitness = oldSpec.Evaluate();
            //    newFit = Evaluate();
            //}
            //IsChecked = true;
        }

        public void RemoveGene(int idx)
        {
            Array.Copy(Genes, idx + 1, Genes, idx, Genes.Length - 1 - idx);
            var lastMove = TMove.Decode((int)Genes[Genes.Length - 1]);
            lastMove.Axis = (lastMove.Axis + 1) % TAffine.N;
            var planeAxes = lastMove.GetPlaneAxes();
            while (planeAxes[0] == lastMove.Axis || planeAxes[1] == lastMove.Axis)
            {
                lastMove.Plane = (lastMove.Plane + 1) % TAffine.Planes.Length;
                planeAxes = TAffine.Planes[lastMove.Plane];
            }
            Genes[Genes.Length - 1] = lastMove.Encode();
            MoveCount--;
        }

        public override double Evaluate()
        {
            //var seq = RubikCube.ReverseSeq;
            //for (int i = 0; i < seq.Count; i++)
            //    Genes[i] = seq[i];
            //MoveCount = Genes.Length;
            //Correct();
            //if (Math.Abs(Genes[Genes.Length - 1] - Genes[Genes.Length - 2]) == 2)
            //    ;
            //specimen.Conjugate();
            //specimen.Mutate(RubikCube.ActCubie);
            Fitness = float.MaxValue;
            //string startCode = cube.Code;
            //for (int j = 0; j < 1; j++)
            {
                var cube = new TRubikCube(RubikCube);
                for (int i = 0; i < Genes.Length; i++)
                {
                    //if (!TRubikGenome.FreeMoves.Contains((int)specimen.Genes[i]))
                    //    ;
                    var move = TMove.Decode((int)Genes[i]);
                    // Final optimalization
                    //if (i == 0)
                    //{
                    //    var actCubie = RubikCube.ActiveCubie;
                    //    move.Slice = actCubie.Position[move.Axis];
                    //    Genes[0] = move.Encode();
                    //}
                    cube.Turn(move);
                    //var cubeCopy = new TRubikCube(cube);
                    //for (int j = i - 1; j >= 0; j--)
                    //    cube.ReTurn(TMove.Decode((int)specimen.Genes[j]));
                    var fitness = cube.Evaluate();
                    if (fitness < Fitness)// && cube.Code != startCode)
                    {
                        Fitness = fitness;
                        //StartIndex = j;
                        MoveCount = i + 1;
                        //if (fitness == 0) break;
                    }
                    //cube = cubeCopy;
                }
            }
            return Fitness;
        }

        public static List<TRubikGenome> SelectRank(List<TRubikGenome> population, int count)
        {
            return population.GetRange(0, count);
        }

        public static List<TRubikGenome> SelectUnique(List<TRubikGenome> population, int count)
        {
            //var seq = RubikCube.ActSeq;
            var sel = new List<TRubikGenome>();
            sel.Add(population[0]);
            for (int i = 1; i < population.Count; i++)
            {
                var specimen = population[i];
                if (specimen.Fitness != sel[sel.Count - 1].Fitness || population.Count - 1 - i < count - sel.Count)
                {
                    //for (int j = 0; j < seq.Count; j++)
                    //    specimen.Genes[j] = seq[j];
                    specimen.Correct();
                    specimen.Macromize();
                    sel.Add(specimen);
                }
                if (sel.Count >= count) break;
            }
            return sel;
        }

#if !MAUI
        public static void Evaluate(List<TRubikGenome> population)
        {
            int[] SsboPopulation = new int[1];
            OpenGL.GenBuffers(1, SsboPopulation);
            OpenGL.BindBufferBase(OpenGL.GL_SHADER_STORAGE_BUFFER, 5, SsboPopulation[0]);
            OpenGL.BindBuffer(OpenGL.GL_SHADER_STORAGE_BUFFER, SsboPopulation[0]);
            var buffer = new float[(GenesLength + 2) * population.Count];
            var pos = 0;
            for (int i = 0; i < population.Count; i++)
            {
                Array.Copy(population[i].Genes, 0, buffer, pos, GenesLength);
                pos += GenesLength + 2;
            }
            OpenGL.BufferDatafv(OpenGL.GL_SHADER_STORAGE_BUFFER, buffer​, OpenGL.GL_DYNAMIC_DRAW);
            OpenGL.DispatchCompute(population.Count, 1, 1);
            OpenGL.MemoryBarrier(OpenGL.MemoryBarrierFlags.ShaderStorageBarrierBit);
            OpenGL.GetBufferSubDatafv(OpenGL.GL_SHADER_STORAGE_BUFFER, 0, buffer);
            pos = GenesLength;
            for (int i = 0; i < population.Count; i++)
            {
                population[i].MoveCount = (int)buffer[pos++];
                population[i].Fitness = buffer[pos++];
                pos += GenesLength;
            }
        }
#endif

    }

}
