using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TGL
{
    public class TKeyFrame
    {
        public TObject3D Bone = new TObject3D();
        public int FrameCount = 1;
        //public int FrameNo;
    }

    public class TAnimation
    {
        public string Name;
        public int Index;
        public bool Enabled;
        public List<TKeyFrame> Keys = new List<TKeyFrame>();
        int KeyNo;
        int FrameCount;
        int FrameNo;
        TObject3D Control;

        public void Animate()
        {
            FrameNo++;
            if (FrameNo >= FrameCount)
            {
                FrameNo = 0;
                KeyNo++;
            }
            if (KeyNo >= Keys.Count)
                KeyNo = 0;
            else
            {
                TKeyFrame key = Keys[KeyNo];
                FrameCount = key.FrameCount;
            }
            var ratio = FrameCount > 1 ? FrameNo / (float)(FrameCount - 1) : 1;

            //if (KeyNo > 0)
            //{
            //Control.Interpolate(key.Prev.Skeleton, key.Skeleton, FrameNo / (double)FrameCount);
            //Control.Interpolate(Index, KeyNo, ratio);
            //}
            Control.Interpolate(Index, KeyNo, ratio);
        }

        //void Interpolate(int keyNo, float ratio)
        //{
        //    if (keyNo < Keys.Count)
        //    {
        //        var startSkel = keyNo == 0 ? Control : Keys[keyNo - 1].Bone;
        //        var endSkel = Keys[keyNo].Bone;
        //        Control.Origin = startSkel.Origin + (endSkel.Origin - startSkel.Origin) * ratio;
        //        Control.Rotation = startSkel.Rotation + (endSkel.Rotation - startSkel.Rotation) * ratio;
        //        //Shear = startSkel.Shear + (endSkel.Shear - startSkel.Shear) * ratio;
        //        //Maps = null;
        //        Control.Scale = startSkel.Scale + (endSkel.Scale - startSkel.Scale) * ratio;
        //    }
        //    for (int i = 0; i < Control.Children.Count; i++)
        //    {
        //        var anims = Control.Children[i].Animations;
        //        if (anims != null && anims[Index] != null)
        //            anims[Index].Interpolate(keyNo, ratio);
        //    }
        //}

        public void Accumulate()
        {
            if (Control != null)
            {
                var key = new TKeyFrame();
                key.Bone = Control.Copy();
                Keys.Add(key);
            }
        }

        public void Play()
        {
            Enabled = true;
            KeyNo = 0;
            FrameNo = 0;
        }

        public void Stop()
        {
            Enabled = false;
        }

        public TAnimation(TObject3D control)
        {
            Control = control;
        }


    }
}
