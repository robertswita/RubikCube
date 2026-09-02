using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace RubikCube
{
    public enum TAnimState { Idle, Running, JustFinished }

    public class TRubikAnimator
    {
        TRubikCube Cube;
        public List<TMove> Moves = new();
        public int MoveNo;
        public int FrameNo;
        public int FrameCount = 10;
        public TAnimState State = TAnimState.Idle;

        private TShape ActiveSlice;

        public void Tick()
        {
            if (State != TAnimState.Running) return;

            TMove move = Moves[MoveNo];
            if (FrameNo == 0)
                AttachSlice(move);

            FrameNo++;
            if (FrameNo <= FrameCount)
            {
                double angle = 90 * move.Angle;
                if (angle > 180) angle -= 360;
                angle *= (double)FrameNo / FrameCount;
                ActiveSlice.Transform = TAffine.CreateRotation(move.Plane, angle);
                return;
            }

            DetachSlice();
            Cube.Turn(move);
            FrameNo = 0;
            MoveNo++;

            if (MoveNo >= Moves.Count)
                State = TAnimState.JustFinished;
        }

        public void Animate(TRubikCube cube, IEnumerable<TMove> moves)
        {
            Cube = cube;
            Moves.Clear();
            Moves.AddRange(moves);
            MoveNo = 0;
            FrameNo = 0;
            State = TAnimState.Running;
        }

        public void Reset()
        {
            Moves.Clear();
            MoveNo = 0;
            State = TAnimState.Idle;
        }

        private void AttachSlice(TMove move)
        {
            if (ActiveSlice?.Parent != null)
                DetachSlice();
            ActiveSlice = new TShape();
            foreach (var cubie in Cube.Cubies)
                if (cubie.GetPos(move.Axis) == move.Slice)
                    cubie.Parent = ActiveSlice;
            ActiveSlice.Parent = Cube;
        }

        private void DetachSlice()
        {
            for (int i = ActiveSlice.Children.Count - 1; i >= 0; i--)
                ActiveSlice.Children[i].Parent = Cube;
            ActiveSlice.Parent = null;
        }
    }
}
