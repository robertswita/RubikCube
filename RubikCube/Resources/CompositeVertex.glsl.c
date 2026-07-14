#version 430
// Full-screen triangle generated from gl_VertexID (no vertex buffer needed). Draw with
// glDrawArrays(GL_TRIANGLES, 0, 3). Covers the whole viewport; vUv spans [0,1].
out vec2 vUv;
void main()
{
    vec2 p = vec2((gl_VertexID << 1) & 2, gl_VertexID & 2);   // (0,0), (2,0), (0,2)
    vUv = p;
    gl_Position = vec4(p * 2.0 - 1.0, 0.0, 1.0);
}
