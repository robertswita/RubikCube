using System;
using System.Collections.Generic;
using System.Linq;
using GA;
using TGL;

namespace RubikCube
{
    public class TRubikGenome: TChromosome
    {
        public int StartIndex;
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
            var geneIdx = Rnd.Next(Genes.Length / 2);
            for (int i = 1; i <= geneIdx; i++)
            {
                var move = TMove.Decode((int)Genes[geneIdx - i]);
                move.Angle = 2 - move.Angle;
                Genes[geneIdx + i] = move.Encode();
            }
            var lastMove = TMove.Decode((int)Genes[geneIdx]);
            lastMove.Angle = 2 - lastMove.Angle;
            Genes[2 * geneIdx + 1] = lastMove.Encode();
            //var geneIdx = Rnd.Next(Genes.Length - 4);
            //for (int i = 0; i < 2; i++)
            //{
            //    var move = TMove.Decode((int)Genes[geneIdx + i]);
            //    move.Angle = 2 - move.Angle;
            //    Genes[geneIdx + 4 - i] = move.Encode();
            //}
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


        public override void Mutate()
        {
            //var geneIdx = Rnd.Next(Genes.Length);
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
            var seq = RubikCube.ActSeq;
            for (int i = 0; i < seq.Count; i++)
            {
                Genes[i] = seq[i];
                //if (!FreeMoves.Contains((int)Genes[i]))
                //    ;
            }
            Conjugate();
            for (int i = StartIndex; i < seq.Count; i++)
                Genes[i] = seq[i];
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
            //Array.Copy(Genes, child.Genes, splitIdx);
            //Array.Copy(other.Genes, splitIdx, child.Genes, splitIdx, Genes.Length - splitIdx);

            splitIdx /= 2;
            Array.Copy(Genes, child.Genes, splitIdx);
            Array.Copy(other.Genes, splitIdx, child.Genes, splitIdx, Genes.Length / 2 - splitIdx);
            //var revMoves = new List<TMove>();
            //for (int i = splitIdx - 1; i >= 0; i--)
            //{
            //    var move = TMove.Decode((int)child.Genes[i]);
            //    move.Angle = 2 - move.Angle;
            //    revMoves.Add(move);
            //}
            //for (int i = Genes.Length / 2 - 1; i >= splitIdx; i--)
            //{
            //    var move = TMove.Decode((int)child.Genes[i]);
            //    move.Angle = 2 - move.Angle;
            //    revMoves.Add(move);
            //}
            //for (int i = 0; i < revMoves.Count; i++)
            //    child.Genes[Genes.Length / 2 + i] = revMoves[i].Encode();

            var pos = Genes.Length / 2;
            for (int i = splitIdx - 1; i >= 0; i--)
                child.Genes[pos++] = child.Genes[i] + 2 * (1 - child.Genes[i] % 3);
            for (int i = Genes.Length / 2 - 1; i >= splitIdx; i--)
                child.Genes[pos++] = child.Genes[i] + 2 * (1 - child.Genes[i] % 3);

            //child.Correct(); 
            //var seq = RubikCube.ReverseSeq;
            //for (int i = 0; i < seq.Count; i++)
            //    Genes[i] = seq[i];
            //for (int i = 0; i < 5; i++)
            //    child.Conjugate();
            return child;
        }

        //bool IsChecked;
        public override void Correct()
        {
            //var fitness = Evaluate();
            //var oldGenes = (double[])Genes.Clone();
            //if (IsChecked) return;
            for (int idx = StartIndex; idx < MoveCount; idx++)
            {
                var move = TMove.Decode((int)Genes[idx]);
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
            Fitness = double.MaxValue;
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
                    double fitness = cube.Evaluate();
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


    }

}
