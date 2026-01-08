using System;
using System.Collections.Generic;
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

        public override void Mutate()
        {
            //var idx = Rnd.Next(Genes.Length) / 2;
            //for (int i = 0; i < idx; i++)
            //{
            //    var move = TMove.Decode((int)Genes[i]);
            //    move.Angle = 2 - move.Angle;
            //    Genes[2 * idx - i] = move.Encode();
            //}
            var geneIdx = Rnd.Next(Genes.Length / 2);

            for (int i = 1; i <= geneIdx; i++)
            {
                var move = TMove.Decode((int)Genes[geneIdx - i]);
                move.Angle = 2 - move.Angle;
                Genes[geneIdx + i] = move.Encode();
            }

            //var key = TMove.Decode((int)Genes[idx]);
            ////key.Axis = max;
            //if (key.Slice < TRubikCube.C)
            //    key.Slice = FreeGenes[0] / 9;
            //else
            //    key.Slice = TRubikCube.N - 1 - FreeGenes[0] / 9;
            //Genes[idx] = key.Encode();



            //var idx = 2 + Rnd.Next(Genes.Length - 7) / 2;
            //for (int i = 0; i < idx; i++)
            //{
            //    var move = TMove.Decode((int)Genes[i]);
            //    move.Angle = 2 - move.Angle;
            //    Genes[2 * idx + 3 - i] = move.Encode();
            //}
            //var tmp = Genes[idx];
            //Genes[idx] = Genes[idx + 1];
            //Genes[idx + 1] = tmp;
            //for (int i = 0; i < 4; i++)
            //{
            //    Genes[idx + 2 + i] = Genes[idx - 2 + i];
            //}
        }

        public override TChromosome Crossover(TChromosome other, int splitIdx)
        {
            var child = new TRubikGenome();
            Array.Copy(Genes, child.Genes, splitIdx);
            Array.Copy(other.Genes, splitIdx, child.Genes, splitIdx, Genes.Length - splitIdx);
            child.Check();
            return child;
        }

        bool IsChecked;
        public void Check()
        {
            if (IsChecked) return;
            for (int idx = 1; idx < Genes.Length; idx++)
            {
                var move = TMove.Decode((int)Genes[idx]);
                for (int i = idx - 1; i >= 0; i--)
                {
                    var prevMove = TMove.Decode((int)Genes[i]);
                    if (prevMove.Axis != move.Axis) break;
                    if (prevMove.Plane != move.Plane) break;
                    if (prevMove.Slice == move.Slice)
                    {
                        move.Angle = (move.Angle + prevMove.Angle + 2) % 4 - 1;
                        Genes[i] = move.Encode();
                        ShiftLeftAt(idx);
                        idx--;
                        if (move.Angle < 0)
                        {
                            ShiftLeftAt(i);
                            idx--;
                        }
                        break;
                    }
                }
            }
            IsChecked = true;
        }

        public void ShiftLeftAt(int idx)
        {
            Array.Copy(Genes, idx + 1, Genes, idx, Genes.Length - 1 - idx);
            var move = TMove.Decode((int)Genes[Genes.Length - 2]);
            move.Axis = (move.Axis + 1) % 4;
            var planeAxes = move.GetPlaneAxes();
            while (planeAxes[0] == move.Axis || planeAxes[1] == move.Axis)
            {
                move.Plane = (move.Plane + 1) % TAffine.Planes.Length;
                planeAxes = TAffine.Planes[move.Plane];
            }
            //move.Angle = 2 - move.Angle;
            Genes[Genes.Length - 1] = move.Encode();
            //MutateGene(Genes.Length - 1);
            //MovesCount--;
        }

        //public List<TMove> Decode()
        //{
        //    var moves = new List<TMove>();
        //    for (int i = 0; i < Genes.Length; i++)
        //        moves.Add(TMove.Decode((int)Genes[i]));
        //    return moves;
        //}

        //public string Code
        //{
        //    get
        //    {
        //        var code = "";
        //        for (int i = 0; i < Genes.Length; i++)
        //            code += (char)Genes[i];
        //        return code;
        //    }
        //}
    }

}
