using System;
using System.Collections.Generic;
using System.Linq;
using GA;
using TGL;

namespace RubikCube
{
    public class TRubikGenome: TChromosome
    {
        public int MovesCount;
        public static List<int> FreeMoves;
        public static TRubikCube RubikCube;

        public TRubikGenome(): base()
        {
            //Correct();
            //var geneIdx = Rnd.Next(Genes.Length - 3);
            //Commute(geneIdx);
            //Mutate();
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

        public void Conjugate(int geneIdx)
        {
            //Check();

            for (int i = 1; i <= geneIdx; i++)
            {
                var move = TMove.Decode((int)Genes[geneIdx - i]);
                move.Angle = 2 - move.Angle;
                Genes[geneIdx + i] = move.Encode();
            }
        }

        public void Commute(int geneIdx)
        {
            //Check();
            //var geneIdx = Rnd.Next(Genes.Length - 3);
            for (int i = 0; i < 2; i++)
            {
                var move = TMove.Decode((int)Genes[geneIdx + i]);
                move.Angle = 2 - move.Angle;
                Genes[geneIdx + 2 + i] = move.Encode();
            }
        }


        public override void Mutate()
        {
            var geneIdx = Rnd.Next(Genes.Length / 2);
            Conjugate(geneIdx);
            //Commute(geneIdx);
        }

        public override TChromosome Crossover(TChromosome other, int splitIdx)
        {
            var child = new TRubikGenome();
            Array.Copy(Genes, child.Genes, splitIdx);
            Array.Copy(other.Genes, splitIdx, child.Genes, splitIdx, Genes.Length - splitIdx);
            child.Correct();
            return child;
        }

        //bool IsChecked;
        public void Correct()
        {
            //if (IsChecked) return;
            for (int idx = 1; idx < Genes.Length; idx++)
            {
                var move = TMove.Decode((int)Genes[idx]);
                for (int prevIdx = idx - 1; prevIdx >= 0; prevIdx--)
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
        }

        public override double Evaluate()
        {
            //Correct();
            //specimen.Conjugate();
            //specimen.Mutate(RubikCube.ActCubie);
            Fitness = double.MaxValue;
            var cube = new TRubikCube(RubikCube);
            //string startCode = cube.Code;
            for (int i = 0; i < Genes.Length; i++)
            {
                //if (!TRubikGenome.FreeMoves.Contains((int)specimen.Genes[i]))
                //    ;
                var move = TMove.Decode((int)Genes[i]);
                // Final optimalization
                if (i == 0)
                {
                    var actCubie = RubikCube.ActiveCubie;
                    move.Slice = actCubie.Position[move.Axis];
                    Genes[0] = move.Encode();
                }
                cube.Turn(move);
                //var cubeCopy = new TRubikCube(cube);
                //for (int j = i - 1; j >= 0; j--)
                //    cube.ReTurn(TMove.Decode((int)specimen.Genes[j]));
                double fitness = cube.Evaluate();
                if (fitness < Fitness)// && cube.Code != startCode)
                {
                    Fitness = fitness;
                    MovesCount = i + 1;
                    //if (fitness == 0) break;
                }
                //cube = cubeCopy;
            }
            return Fitness;
        }


    }

}
