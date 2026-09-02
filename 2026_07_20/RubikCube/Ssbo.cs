using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace RubikCube
{
    public unsafe class Ssbo
    {
        static uint BindingsCount;
        public uint Id;
        public int Size;
        public uint Type;
        public uint Binding;

        // Call once before creating a batch of buffers so bindings are assigned 0, 1, 2, ...
        // in creation order (must match the layout(binding = ...) declarations in Setup.glsl.c).
        public static void ResetBinding() => BindingsCount = 0;

        // Reserve the buffer's id and bind it to its slot. The size and contents are set later by
        // Update(size, data), which allocates (and reallocates) the backing store.
        public Ssbo(uint type = OpenGL.GL_SHADER_STORAGE_BUFFER)
        {
            Type = type;
            Binding = BindingsCount++;
            uint id;
            OpenGL.GenBuffers(1, &id);
            Id = id;
            Bind();
        }

        // Rebind to this buffer's own slot (used when ping-pong temporarily binds another buffer here).
        public void Bind() => OpenGL.BindBufferBase(Type, Binding, Id);

        public void SetData(void* data)
        {
            OpenGL.BindBuffer(Type, Id);
            OpenGL.BufferData(Type, Size, data, OpenGL.GL_DYNAMIC_COPY);
        }

        // Reuse this buffer with new contents/size (glBufferData reallocates the store); the id and
        // binding are preserved, so there is no buffer churn between GA runs.
        public void Update(int size, void* data)
        {
            Size = size;
            SetData(data);
        }

        public void GetBufferData(void* buffer)
        {
            OpenGL.BindBuffer(Type, Id);
            OpenGL.GetBufferSubData(Type, 0, Size, buffer);
        }
    }
}
