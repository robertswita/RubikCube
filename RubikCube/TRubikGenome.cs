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
            Check();
            var geneIdx = Rnd.Next(Genes.Length / 2);

            for (int i = 1; i <= geneIdx; i++)
            {
                var move = TMove.Decode((int)Genes[geneIdx - i]);
                move.Angle = 2 - move.Angle;
                Genes[geneIdx + i] = move.Encode();
            }
        }


        public override void Mutate()
        {
            //MutateGene(Rnd.Next(Genes.Length));
            Check();
            var geneIdx = Rnd.Next(Genes.Length / 2);
            for (int i = 1; i <= geneIdx; i++)
            {
                var move = TMove.Decode((int)Genes[geneIdx - i]);
                move.Angle = 2 - move.Angle;
                Genes[geneIdx + i] = move.Encode();
            }
        }

        public override TChromosome Crossover(TChromosome other, int splitIdx)
        {
            var child = new TRubikGenome();
            Array.Copy(Genes, child.Genes, splitIdx);
            Array.Copy(other.Genes, splitIdx, child.Genes, splitIdx, Genes.Length - splitIdx);
            //child.Check();
            return child;
        }

        //bool IsChecked;
        public void Check()
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

    }

}
