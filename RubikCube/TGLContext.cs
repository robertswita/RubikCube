/**********************************************************
Autor: Robert Świta
Politechnika Koszalińska
Katedra Systemów Multimedialnych i Sztucznej inteligencji
***********************************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;

namespace TGL
{
    public class TGLContext
    {
        IntPtr HRC;
        IntPtr HDC;
        public TGLView View;
        public const int MAX_LIGHTS = 10;
        public const int MAX_BONES = 4;
        public const int AASamples = 4;
        int[] UboCamera = new int[1];
        int[] UboBones = new int[1];
        int[] UboLights = new int[1];
        int[] FboMultisample = new int[1];
        int[] RboMultisample;
        int[] RboDepth;

        // HRC to uchwyt do kontekstu renderowania tworzony na podstawie kontekstu 
        // urządzenia DC enkapsulowanego przez klasę Graphics. Należy określić format piksela DC. 
        // Win32.ChoosePixelFormat dostarcza indeksu formatu najbliższego do naszych żądań. 
        // Następnie musimy ustawić ten format poleceniem SetPixelFormat.
        public IntPtr Handle
        {
            get
            {
                if (HRC == IntPtr.Zero)
                {
                    HDC = View.CreateGraphics().GetHdc();
                    var pfd = new Win32.PIXELFORMATDESCRIPTOR();
                    var pixelFormat = new int[1];
                    pixelFormat[0] = Win32.ChoosePixelFormat(HDC, pfd);
                    Win32.SetPixelFormat(HDC, pixelFormat[0], pfd);
                    HRC = Win32.wglCreateContext(HDC);
                    Win32.wglMakeCurrent(HDC, HRC);
                    if (OpenGL.ChoosePixelFormat(HDC, pixelFormat))
                    {
                        Win32.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
                        Win32.wglDeleteContext(HRC);
                        View.Recreate();
                        HDC = View.CreateGraphics().GetHdc();
                        Win32.SetPixelFormat(HDC, pixelFormat[0], pfd);
                        HRC = Win32.wglCreateContext(HDC);
                        Win32.wglMakeCurrent(HDC, HRC);
                        OpenGL.glEnable(OpenGL.GL_MULTISAMPLE);
                    }
                    else
                    {
                        OpenGL.GenFramebuffers(1, FboMultisample);
                        RboMultisample = new int[1];
                        OpenGL.GenRenderbuffers(1, RboMultisample);
                        RboDepth = new int[1];
                        OpenGL.GenRenderbuffers(1, RboDepth);
                    }

                    var gpuProgram = OpenGL.CreateProgram();
                    OpenGL.AttachShader(gpuProgram, CreateShader(OpenGL.GL_VERTEX_SHADER));
                    OpenGL.AttachShader(gpuProgram, CreateShader(OpenGL.GL_FRAGMENT_SHADER));
                    OpenGL.LinkProgram(gpuProgram);
                    OpenGL.UseProgram(gpuProgram);
                    OpenGL.GenBuffers(1, UboCamera);
                    OpenGL.BindBufferBase(OpenGL.GL_UNIFORM_BUFFER, 0, UboCamera[0]);
                    OpenGL.GenBuffers(1, UboBones);
                    //OpenGL.BindBufferBase(OpenGL.GL_SHADER_STORAGE_BUFFER, 1, SsboBones[0]);
                    OpenGL.BindBufferBase(OpenGL.GL_UNIFORM_BUFFER, 1, UboBones[0]);
                    OpenGL.GenBuffers(1, UboLights);
                    OpenGL.BindBufferBase(OpenGL.GL_UNIFORM_BUFFER, 2, UboLights[0]);
                }
                return HRC;
            }
        }

        int CreateShader(int shaderType)
        {
            var shader = OpenGL.CreateShader(shaderType);
            if (shaderType == OpenGL.GL_VERTEX_SHADER)
                OpenGL.ShaderSource(shader, RubikCube.Properties.Resources.Vertex_glsl);
            else
                OpenGL.ShaderSource(shader, RubikCube.Properties.Resources.Fragment_glsl);
            OpenGL.CompileShader(shader);
            var status = new int[1];
            OpenGL.GetShader(shader, OpenGL.GL_COMPILE_STATUS, status);
            if (status[0] == 0)
            {
                var maxLength = new int[1];
                OpenGL.GetShader(shader, OpenGL.GL_INFO_LOG_LENGTH, maxLength);
                var log = new StringBuilder(maxLength[0]);
                OpenGL.GetShaderInfoLog(shader, maxLength[0], IntPtr.Zero, log);
            }
            return shader;
        }

        public virtual void Resize()
        {
            var vp = View.ClientRectangle;
            Win32.wglMakeCurrent(HDC, HRC);
            if (FboMultisample[0] > 0)
            {
                OpenGL.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, FboMultisample[0]);
                //var tex = new int[1];
                //OpenGL.glGenTextures(1, tex);
                //OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D_MULTISAMPLE, tex[0]);
                //OpenGL.glTexImage2DMultisample(OpenGL.GL_TEXTURE_2D_MULTISAMPLE, samples, OpenGL.GL_RGB, vp.Width, vp.Height, OpenGL.GL_TRUE);
                //OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D_MULTISAMPLE, 0);
                //OpenGL.FramebufferTexture2D(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT0, OpenGL.GL_TEXTURE_2D_MULTISAMPLE, tex[0], 0);
                OpenGL.BindRenderbuffer(OpenGL.GL_RENDERBUFFER, RboMultisample[0]);
                OpenGL.RenderbufferStorageMultisample(OpenGL.GL_RENDERBUFFER, AASamples, OpenGL.GL_RGB8, vp.Width, vp.Height);
                OpenGL.FramebufferRenderbuffer(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT0, OpenGL.GL_RENDERBUFFER, RboMultisample[0]);
                OpenGL.BindRenderbuffer(OpenGL.GL_RENDERBUFFER, RboDepth[0]);
                OpenGL.RenderbufferStorageMultisample(OpenGL.GL_RENDERBUFFER, AASamples, OpenGL.GL_DEPTH24_STENCIL8, vp.Width, vp.Height);
                OpenGL.FramebufferRenderbuffer(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_DEPTH_STENCIL_ATTACHMENT, OpenGL.GL_RENDERBUFFER, RboDepth[0]);
            }
            OpenGL.glViewport(vp.Left, vp.Top, vp.Width, vp.Height);
            View.Invalidate();
        }

        public virtual void DrawView()
        {
            var vp = View.ClientRectangle;
            var backColor = View.BackColor;
            Win32.wglMakeCurrent(HDC, HRC);
            OpenGL.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, FboMultisample[0]);
            OpenGL.glClearColor(backColor.R / 255f, backColor.G / 255f, backColor.B / 255f, 1);
            OpenGL.glClear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT | OpenGL.GL_STENCIL_BUFFER_BIT);
            Init();
            DrawScene();
            if (FboMultisample[0] > 0)
            {
                OpenGL.BindFramebuffer(OpenGL.GL_READ_FRAMEBUFFER, FboMultisample[0]);
                OpenGL.BindFramebuffer(OpenGL.GL_DRAW_FRAMEBUFFER, 0);
                OpenGL.BlitFramebuffer(0, 0, vp.Width, vp.Height, 0, 0, vp.Width, vp.Height, OpenGL.GL_COLOR_BUFFER_BIT, OpenGL.GL_NEAREST);
                OpenGL.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);
            }
            Win32.SwapBuffers(HDC);
        }

        void InitScene()
        {
            var buffer = new float[20];
            Array.Copy(Camera.Projection.Data, 0, buffer, 0, 16);
            Array.Copy(Camera.WorldTransform.Data, 12, buffer, 16, 3);
            OpenGL.BindBuffer(OpenGL.GL_UNIFORM_BUFFER, UboCamera[0]);
            OpenGL.BufferDatafv(OpenGL.GL_UNIFORM_BUFFER, buffer, OpenGL.GL_STATIC_DRAW);
            var lightsCount = 0;
            buffer = new float[MAX_LIGHTS * 20 + 4];
            for (int i = 0; i < Camera.Scene.Lights.Count; i++)
            {
                var start = 20 * i;
                TLight light = Camera.Scene.Lights[i];
                if (!light.IsEnabled)
                    continue;
                lightsCount++;
                light.WorldTransform = null;
                var position = light.WorldTransform * light.Origin;
                if (light.IsDirectional)
                {
                    position.Norm = 1;
                    position *= -1;
                }
                else
                    buffer[start + 3] = 1;
                Array.Copy(position.Data, 0, buffer, start, 3);
                Array.Copy(light.Ambient.Data, 0, buffer, start + 4, 3);
                Array.Copy(light.Diffuse.Data, 0, buffer, start + 8, 3);
                Array.Copy(light.Specular.Data, 0, buffer, start + 12, 3);
                Array.Copy(light.AttCoeff.Data, 0, buffer, start + 16, 3);
            }
            buffer[MAX_LIGHTS * 20] = lightsCount;
            OpenGL.BindBuffer(OpenGL.GL_UNIFORM_BUFFER, UboLights[0]);
            OpenGL.BufferDatafv(OpenGL.GL_UNIFORM_BUFFER, buffer, OpenGL.GL_STATIC_DRAW);
        }

        public TCamera Camera = new TCamera();
        void DrawScene()
        {
            if (Camera.Scene != null)
            {
                InitScene();
                //CalcWorld(Camera.Root);
                DrawObject(Camera.Root);
            }
        }

        //void CalcWorld(TObject3D obj)
        //{
        //    if (obj.Parent != null)
        //        obj.WorldTransform = obj.Parent.WorldTransform * obj.Transform;
        //    foreach (var child in obj.Children)
        //        CalcWorld(child);
        //}

        [StructLayout(LayoutKind.Sequential)]
        struct Element
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] Coord;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public float[] TexCoord;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_BONES)]
            public float[] Bones;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_BONES)]
            public float[] Weights;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] Normal;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] Tangent;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] Bitangent;
        };

        void DrawObject(TObject3D obj)
        {
            if (obj.Parent != null)
                obj.WorldTransform = obj.Parent.WorldTransform * obj.Transform;
            if (obj.Faces.Count > 0)
            {
                foreach (var map in obj.Maps)
                {
                    if (map.DisplayList == 0)
                    {
                        var vao = new int[1];
                        OpenGL.GenVertexArrays(1, vao);
                        map.DisplayList = vao[0];
                        OpenGL.BindVertexArray(map.DisplayList);
                        var vbo = new int[1];
                        OpenGL.GenBuffers(1, vbo);
                        OpenGL.BindBuffer(OpenGL.GL_ARRAY_BUFFER, vbo[0]);
                        var elType = typeof(Element);
                        var elSize = Marshal.SizeOf(elType);
                        var bufSize = 3 * map.Faces.Count * elSize;
                        IntPtr buf = Marshal.AllocHGlobal(bufSize);
                        for (int i = 0; i < map.Faces.Count; i++)
                        {
                            var face = map.Faces[i];
                            for (int j = 0; j < face.Vertices.Count; j++)
                            {
                                var v = face.Vertices[j];
                                var elem = new Element();
                                elem.Coord = v.Data;
                                elem.Normal = face.Smooth ? v.Normal.Data : face.Normal.Data;
                                elem.Tangent = face.TB.Cols[0].Data;
                                elem.Bitangent = face.TB.Cols[1].Data;
                                elem.TexCoord = new float[2];
                                if (face.TexVertices.Count > 0)
                                    Array.Copy(face.TexVertices[j].Data, elem.TexCoord, 2);
                                else
                                    Array.Copy(v.TexCoord.Data, elem.TexCoord, 2);
                                elem.Bones = new float[MAX_BONES];
                                elem.Weights = new float[MAX_BONES];
                                var weights = v.Weights.ToArray();
                                var bones = v.Bones.ToArray();
                                Array.Sort(weights, bones);
                                for (int k = bones.Length - 1; k >= 0; k--)
                                {
                                    var idx = bones.Length - 1 - k;
                                    if (idx >= MAX_BONES)
                                        break;
                                    elem.Bones[idx] = obj.Bones.IndexOf(bones[k]);
                                    elem.Weights[idx] = weights[k];
                                }
                                Marshal.StructureToPtr(elem, buf + (3 * i + j) * elSize, false);
                            }
                        }
                        OpenGL.BufferData(OpenGL.GL_ARRAY_BUFFER, bufSize, buf, OpenGL.GL_STATIC_DRAW);
                        Marshal.FreeHGlobal(buf);
                        var fields = elType.GetFields();
                        var offset = 0;
                        for (int i = 0; i < fields.Length; i++)
                        {
                            var notLast = i < fields.Length - 1;
                            var next = notLast ? (int)Marshal.OffsetOf(elType, fields[i + 1].Name) : elSize;
                            var size = (next - offset) / sizeof(float);
                            OpenGL.VertexAttribPointer(i, size, OpenGL.GL_FLOAT, false, elSize, (IntPtr)offset);
                            OpenGL.EnableVertexAttribArray(i);
                            offset = next;
                        }
                    }
                    OpenGL.BindVertexArray(map.DisplayList);
                    var uniformLoc = 0;
                    OpenGL.UniformMatrix4fv(uniformLoc++, 1, false, obj.WorldTransform.Data);
                    if (map.Material != null)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            LoadTexture(map.Material.Textures[i], i);
                            OpenGL.Uniform1i(uniformLoc++, i);
                        }
                        OpenGL.Uniform1f(uniformLoc++, map.Material.Shininess);
                    }
                    var boneMatrix = new TMatrix(16, obj.Bones.Count);
                    for (int i = 0; i < obj.Bones.Count; i++)
                    {
                        var bone = obj.Bones[i];
                        boneMatrix.Cols[i] = bone.WorldTransform * bone.BindPoseInv;
                    }
                    OpenGL.BindBuffer(OpenGL.GL_UNIFORM_BUFFER, UboBones[0]);
                    OpenGL.BufferDatafv(OpenGL.GL_UNIFORM_BUFFER, boneMatrix.Data​, OpenGL.GL_DYNAMIC_DRAW);
                    OpenGL.glDrawArrays(OpenGL.GL_TRIANGLES, 0, 3 * map.Faces.Count);
                }
            }
            foreach (var child in obj.Children)
            {
                //obj.Children.Sort();
                DrawObject(child);
            }
        }

        void LoadTexture(TMaterial.TTexture texture, int unit)
        {
            OpenGL.ActiveTexture(OpenGL.GL_TEXTURE0 + unit);
            if (texture.DisplayList <= 0)
            {
                texture.DisplayList *= -1;
                var to = new int[] { texture.DisplayList };
                OpenGL.glDeleteTextures(1, to);
                OpenGL.glGenTextures(1, to);
                texture.DisplayList = to[0];
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, texture.DisplayList);
                var bmp = texture.Map;
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                var bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                OpenGL.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, 4, bmp.Width, bmp.Height, 0, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, bmpData.Scan0);
                OpenGL.GenerateMipmap(OpenGL.GL_TEXTURE_2D);
                bmp.UnlockBits(bmpData);
            }
            else
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, texture.DisplayList);
        }

        bool _Init;
        void Init()
        {
            if (!_Init)
            {
                OpenGL.glEnable(OpenGL.GL_DEPTH_TEST);
                //OpenGL.glEnable(OpenGL.GL_CULL_FACE); 
                //OpenGL.glPolygonMode(OpenGL.GL_FRONT_AND_BACK, OpenGL.GL_LINE);
                //OpenGL.glEnable(OpenGL.GL_BLEND);
                //OpenGL.glBlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);
                //OpenGL.glEnable(OpenGL.GL_POLYGON_SMOOTH);
                _Init = true;
            }
        }
    };

}
