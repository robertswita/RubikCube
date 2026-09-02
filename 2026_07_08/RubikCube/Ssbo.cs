using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TGL;
using static TGL.OpenGL;

namespace RubikCube
{
    public unsafe class Ssbo
    {
        static uint Binding; 
        public uint Id;
        public int Size;
        public uint Type;
        public Ssbo(int size, uint type = OpenGL.GL_SHADER_STORAGE_BUFFER) {
            Size = size;
            Type = type;
            uint id;
            OpenGL.GenBuffers(1, &id);
            Id = id;
            //OpenGL.BindBuffer(Type, Id[0]);
            //if (buffer != null)
            //{
            //    var bufferPtr = Marshal.AllocHGlobal(size);
            //    Marshal.Copy(buffer, 0, bufferPtr, buffer.Length);
            //    OpenGL.BufferData(Type, size, bufferPtr, OpenGL.GL_DYNAMIC_COPY);
            //    Marshal.FreeHGlobal(bufferPtr);
            //}
            //else
            //    OpenGL.BufferData(Type, size, IntPtr.Zero, OpenGL.GL_DYNAMIC_COPY);
            OpenGL.BindBufferBase(Type, Binding++, Id);
        }
        public unsafe void GetBufferData(void* buffer)
        {
            OpenGL.BindBuffer(Type, Id);
            OpenGL.GetBufferSubData(Type, 0, Size, buffer);
        }
    }
}
