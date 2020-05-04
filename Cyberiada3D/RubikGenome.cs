using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace Rubik
{
    public class RubikGenome: TChromosome
    {
        public int MovesCount;
        public override TChromosome Cross(TChromosome other, int splitIdx)
        {
            var child = new RubikGenome();
            Array.Copy(Genes, 0, child.Genes, 0, splitIdx);
            Array.Copy(
                other.Genes,
                splitIdx + 1,
                child.Genes,
                splitIdx + 1,
                other.Genes.Length - splitIdx - 1
            );

            child.Correct();

            return child;
        }

        private void RemoveGene(int i)
        {
            Array.Copy(Genes, i + 1, Genes, i, GenesLength - (i + 1));
            var move = new TMove();
            move.Decode((int)Genes[GenesLength - 1]);
            move.Axis = (move.Axis + 1) % 3;
            Genes[GenesLength - 1] = move.Encode();
        }

        public void Correct()
        {
            for (int i = 0; i < GenesLength; i++)
            {
                var move = new TMove();
                move.Decode((int)Genes[i]);

                for (int j = i - 1; j >= 0; j--)
                {
                    var pMove = new TMove();
                    pMove.Decode((int)Genes[j]);

                    if (move.Axis != pMove.Axis)
                    {
                        break;
                    }

                    if (move.SegNo == pMove.SegNo)
                    {
                        var angle = (pMove.Angle + move.Angle + 2) % 4;

                        if (angle == 0)
                        {
                            RemoveGene(j);
                            i--;
                
                        } else
                        {
                            pMove.Angle = angle - 1;
                        }

                        RemoveGene(i);
                        break;
                    }
                }
            }
        }
    }
}
