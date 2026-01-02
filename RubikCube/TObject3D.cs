using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGL
{
    public class TObject3D
    {
        public List<TVertex> Vertices = new List<TVertex>();
        public List<TFace> Faces = new List<TFace>();
        public List<TVector> TexVertices = new List<TVector>();
        public List<TMaterial> Materials = new List<TMaterial>();
        public TVector Scale = new TVector(1, 1, 1);
        public TVector Shear = new TVector();
        public TVector Rotation = new TVector();
        public TVector Origin = new TVector();
        public List<TObject3D> Children = new List<TObject3D>();
        //public TAffine BindPose = new TAffine();
        public TAffine BindPoseInv = new TAffine();
        public List<TObject3D> Bones = new List<TObject3D>();
        public List<TAnimation> Animations;// = new List<TAnimation>();
        public bool Transparent;
        public bool Selected;

        protected List<TMap> _Maps;
        public List<TMap> Maps
        {
            get
            {
                if (_Maps == null)
                {
                    _Maps = new List<TMap>();
                    foreach(var face in Faces)
                    { 
                        var map = _Maps.Find(x => x.Material == face.Material);
                        if (map == null)
                        {
                            map = new TMap();
                            map.Material = face.Material;
                            _Maps.Add(map);
                        }
                        map.Faces.Add(face);
                    }
                }
                return _Maps;
            }
            set { 
                _Maps = value; 
            }
        }

        TObject3D _Parent;
        public virtual TObject3D Parent
        {
            get { return _Parent; }
            set
            {
                if (_Parent != null)
                    _Parent.Children.Remove(this);
                _Parent = value;
                if (_Parent != null)
                    _Parent.Children.Add(this);
            }
        }

        public TObject3D Root
        {
            get
            {
                var root = this;
                while (root.Parent != null)
                    root = root.Parent;
                return root;
            }
        }

        TScene _Scene;
        public TScene Scene
        {
            get { return Root?._Scene; }
            set { _Scene = value; }
        }

        public TAffine Transform
        {
            get
            {
                var transform = TAffine.Translate(Origin);
                transform *= TAffine.RotateZ(Rotation.Z);
                transform *= TAffine.RotateY(Rotation.Y);
                transform *= TAffine.RotateX(Rotation.X);
                transform *= TAffine.Shear(Shear);
                transform *= TAffine.Scale(Scale);
                return transform;
            }

            set
            {
                var transform = value;
                Origin.Assign(transform.Cols[3]);
                Rotation.Z = Math.Atan2(transform[1, 0], transform[0, 0]) * 180 / Math.PI;
                transform = TAffine.RotateZ(-Rotation.Z) * transform;
                Rotation.Y = Math.Atan2(-transform[2, 0], transform[0, 0]) * 180 / Math.PI;
                transform = TAffine.RotateY(-Rotation.Y) * transform;
                Rotation.X = Math.Atan2(transform[2, 1], transform[1, 1]) * 180 / Math.PI;
                transform = TAffine.RotateX(-Rotation.X) * transform;
                Scale.X = transform[0, 0];
                Scale.Y = transform[1, 1];
                Scale.Z = transform[2, 2];
                Shear.X = transform[0, 1] / Scale.Y;
                Shear.Y = transform[0, 2] / Scale.Z;
                Shear.Z = transform[1, 2] / Scale.Z;
            }
        }

        public void Pitch(double angle)
        {
            var transform = TAffine.Translate(Origin);
            transform *= TAffine.Rotate(angle, Transform.Cols[0]);
            transform *= TAffine.Translate(Origin * -1);
            Transform = transform * Transform;
        }

        public void Yaw(double angle)
        {
            var transform = TAffine.Translate(Origin);
            transform *= TAffine.Rotate(angle, Transform.Cols[1]);
            transform *= TAffine.Translate(Origin * -1);
            Transform = transform * Transform;
        }

        public void Roll(double angle)
        {
            var transform = TAffine.Translate(Origin);
            transform *= TAffine.Rotate(angle, Transform.Cols[2]);
            transform *= TAffine.Translate(Origin * -1);
            Transform = transform * Transform;
        }

        public void MoveX(double d) { var v = Transform.Cols[0]; v.Norm = d; Origin += v; }
        public void MoveY(double d) { var v = Transform.Cols[1]; v.Norm = d; Origin += v; }
        public void MoveZ(double d) { var v = Transform.Cols[2]; v.Norm = d; Origin += v; }

        TAffine _WorldTransform;
        public TAffine WorldTransform
        {
            get
            {
                if (_WorldTransform == null)
                {
                    _WorldTransform = Transform;
                    if (Parent != null)
                        _WorldTransform = Parent.WorldTransform * _WorldTransform;
                }
                return _WorldTransform;
            }
            set { _WorldTransform = value; }
        }

        internal void OnCollision(TObject3D obj)
        {
            var shift = obj.Origin - Origin;
            shift.Norm = 0.01 * BBox.Scale.X;
            Origin -= shift;
        }

        TCube bbox;
        public TCube BBox
        {
            get
            {
                if (bbox == null)
                {
                    var pts = new List<TVector>();
                    for (int i = 0; i < Vertices.Count; i++)
                        pts.Add(Vertices[i]);
                    for (int i = 0; i < Children.Count; i++)
                    {
                        var child = Children[i];
                        var childBBox = child.BBox;
                        if (childBBox == null) continue;
                        var lbn = childBBox.LBN;
                        var rtf = childBBox.RTF;
                        for (int j = 0; j < 8; j++)
                        {
                            var v = lbn.Clone();
                            if ((j & 1) != 0) v.X = rtf.X;
                            if ((j & 2) != 0) v.Y = rtf.Y;
                            if ((j & 4) != 0) v.Z = rtf.Z;
                            v = child.Transform * v;
                            pts.Add(v);
                        }
                    }
                    if (pts.Count == 0) return null;
                    bbox = new TCube();
                    for (int i = 0; i < pts.Count; i++)
                    {
                        if (i == 0)
                            bbox.Origin = pts[0];
                        else
                            bbox.Union(pts[i]);
                    }
                }
                return bbox;
            }
            set { bbox = value; }
        }

        public void Interpolate(int animNo, int keyNo, float ratio)
        {
            if (Animations != null)
            {
                var anim = Animations[animNo];
                if (keyNo < anim.Keys.Count)
                {
                    var startSkel = keyNo == 0 ? this : anim.Keys[keyNo - 1].Bone;
                    var endSkel = anim.Keys[keyNo].Bone;
                    Origin = startSkel.Origin + (endSkel.Origin - startSkel.Origin) * ratio;
                    Rotation = startSkel.Rotation + (endSkel.Rotation - startSkel.Rotation) * ratio;
                    Scale = startSkel.Scale + (endSkel.Scale - startSkel.Scale) * ratio;
                    //Shear = startSkel.Shear + (endSkel.Shear - startSkel.Shear) * ratio;
                }
                //else
            }
            for (int i = 0; i < Children.Count; i++)
                Children[i].Interpolate(animNo, keyNo, ratio);
        }

        public TObject3D Copy()
        {
            TObject3D dest = new TObject3D();
            dest.Scale.Assign(Scale);
            dest.Shear.Assign(Shear);
            dest.Rotation.Assign(Rotation);
            dest.Origin.Assign(Origin);
            dest.Vertices = Vertices;
            dest.TexVertices = TexVertices;
            dest.Faces = Faces;
            dest.Bones = Bones;
            dest.Materials = Materials;
            dest.Maps = Maps;
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i].Copy();
                child.Parent = dest;
            }
            return dest;
        }

        public void TesselateConvex()
        {
            foreach (var map in Maps)
                for (int i = map.Faces.Count - 1; i >= 0; i--)
                {
                    var face = map.Faces[i];
                    for (int j = face.Vertices.Count - 1; j >= 3; j--)
                    {
                        var newFace = new TFace();
                        newFace.AddVertex(face.Vertices[0]);
                        newFace.AddVertex(face.Vertices[j - 1]);
                        newFace.AddVertex(face.Vertices[j]);
                        if (face.TexVertices.Count > 0)
                        {
                            newFace.TexVertices.Add(face.TexVertices[0]);
                            newFace.TexVertices.Add(face.TexVertices[j - 1]);
                            newFace.TexVertices.Add(face.TexVertices[j]);
                            face.TexVertices.RemoveAt(face.TexVertices.Count - 1);
                        }
                        newFace.Material = face.Material;
                        face.Vertices[j].Faces.Remove(face);
                        face.Vertices.RemoveAt(j);
                        map.Faces.Add(newFace);
                    }
                }
        }

        public string Name;
        public virtual void ImportFromStream(Stream S) { }
        public void ImportFromFile(String fileName)
        {
            FileStream S = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            ImportFromStream(S);
            S.Close();
        }

        public static TObject3D CreateCube()
        {
            var cube = new TObject3D();
            var lbn = new TVector(-1, -1, -1);
            var rtf = new TVector(1, 1, 1);
            for (int i = 0; i < 8; i++)
            {
                var v = new TVertex();
                v.Index = i;
                v.Assign(lbn);
                for (int j = 0; j < v.Data.Length; j++)
                    if ((i & 1 << j) != 0) v[j] = rtf[j];
                cube.Vertices.Add(v);
            }
            var indices = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                var axis1 = 1 << i;
                var axis2 = 1 << (i + 1) % 3;
                indices.Add(0);
                indices.Add(axis1);
                indices.Add(axis2);
                indices.Add(axis1);
                indices.Add(axis1 + axis2);
                indices.Add(axis2);
            }
            for (int i = indices.Count - 1; i >= 0; i--)
                indices.Add(7 - indices[i]);
            //var mat = new TMaterial();
            TMaterial mat = null;
            //mat.Diffuse.Color = Color.Magenta;
            //cube.Materials.Add(mat);
            cube.TexVertices.Add(new TVector(0, 0));
            cube.TexVertices.Add(new TVector(1, 0));
            cube.TexVertices.Add(new TVector(0, 1));
            cube.TexVertices.Add(new TVector(1, 1));
            var texIndices = new int[] 
                { 0, 1, 2, 1, 3, 2, 
                  1, 3, 0, 3, 2, 0,
                  1, 3, 0, 3, 2, 0,
                  1, 3, 2, 1, 2, 0, 
                  2, 0, 1, 2, 1, 3, 
                  0, 1, 3, 0, 3, 2 };
            for (int i = 0; i < 12; i++)
            {
                var face = new TFace();
                face.Smooth = false;
                face.AddVertex(cube.Vertices[indices[3 * i]]);
                face.AddVertex(cube.Vertices[indices[3 * i + 1]]);
                face.AddVertex(cube.Vertices[indices[3 * i + 2]]);
                if (i % 2 == 0)
                {
                    mat = new TMaterial();
                    mat.Diffuse.Color = Color.Magenta;
                    cube.Materials.Add(mat);
                }
                face.TexVertices.Add(cube.TexVertices[texIndices[3 * i]]);
                face.TexVertices.Add(cube.TexVertices[texIndices[3 * i + 1]]);
                face.TexVertices.Add(cube.TexVertices[texIndices[3 * i + 2]]);
                face.Material = mat;
                cube.Faces.Add(face);
            }
            return cube;
        }

        public static TObject3D CreateSphere()
        {
            var sphere = new TObject3D();
            TMaterial material = new TMaterial();
            sphere.Materials.Add(material);
            material.Diffuse.Color = Color.Magenta;
            const int Count = 15;
            for (int h = 0; h <= Count; h++)
            {
                double phi = Math.PI * h / Count;
                double cosPhi = Math.Cos(phi);
                double sinPhi = Math.Sin(phi);
                for (int w = 0; w <= 2 * Count; w++)
                {
                    double theta = Math.PI * w / Count;
                    var v = new TVertex();
                    v.Index = sphere.Vertices.Count;
                    sphere.Vertices.Add(v);
                    v.X = sinPhi * Math.Cos(theta);
                    v.Y = cosPhi;
                    v.Z = sinPhi * Math.Sin(theta);
                    v.TexCoord.X = phi / Math.PI;
                    v.TexCoord.Y = theta / (2 * Math.PI);
                    if (h > 0 && w > 0)
                    {
                        var idx = sphere.Vertices.Count - 1;
                        var pIdx = idx - (2 * Count + 1);
                        var face = new TFace();
                        sphere.Faces.Add(face);
                        face.Material = material;
                        face.AddVertex(sphere.Vertices[pIdx - 1]);
                        face.AddVertex(sphere.Vertices[idx - 1]);
                        face.AddVertex(v);
                        face.Smooth = true;
                        face = new TFace();
                        sphere.Faces.Add(face);
                        face.Material = material;
                        face.AddVertex(sphere.Vertices[pIdx]);
                        face.AddVertex(sphere.Vertices[pIdx - 1]);
                        face.AddVertex(v);
                        face.Smooth = true;
                    }
                }
            }
            return sphere;
        }
    }
}
