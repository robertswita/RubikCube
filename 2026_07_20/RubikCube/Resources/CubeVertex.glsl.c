#version 430
// Instanced hypercube render. N is injected at compile time (like the GA shaders).
// Base positions are ND (may exceed a vec4), so they are vertex-pulled from an SSBO by
// gl_VertexID; the per-instance ND world transform (M column-major + Origin + alpha) is pulled
// from another SSBO by (gl_InstanceID + uInstanceBase). Projection = orthographic (drop coords >= 3).
#include "Variables.glsl.c"
layout(std430, binding = 8)  buffer Pos    { float basePos[]; };    // N floats per base vertex
layout(std430, binding = 9)  buffer Xforms { float world[];   };    // (N*N + N + 1) floats per instance: M, Origin, alpha
layout(std430, binding = 11) buffer Mats   { vec4 materials[]; };   // per base face: rgb = specular, w = shininess

// Instances are packed opaque-first then transparent; each pass draws its slice via this base offset
// added to gl_InstanceID (avoids a base-instance draw call, which is not loaded).
layout(location = 0) uniform int uInstanceBase;

layout(location = 0) in vec2 uv;
layout(location = 1) in vec4 color;

out vec2 fUv;
out vec4 fCol;
out vec3 fPos;
flat out vec3 fNormal;
flat out vec3 fTangent;
flat out vec3 fBitangent;
flat out vec3 fMatSpec;
flat out float fShininess;
flat out float fAlpha;

const int STRIDE = N * N + N + 1;   // per-instance floats in the Xforms SSBO

// Orthographic ND->3D projection of base vertex `vid` for instance `inst`: keep the first up-to-3
// coords of the ND world position (M * p + Origin), M stored column-major.
vec3 project(int vid, int inst)
{
    int vb = vid * N;
    int mb = inst * STRIDE;
    vec3 p = vec3(0.0);
    for (int r = 0; r < min(N, 3); r++)
    {
        float s = world[mb + N * N + r];                    // Origin[r]
        for (int c = 0; c < N; c++)
            s += world[mb + c * N + r] * basePos[vb + c];   // M[r,c] * p[c]
        p[r] = s;
    }
    return p;
}

void main()
{
    int inst = gl_InstanceID + uInstanceBase;
    vec3 p3 = project(gl_VertexID, inst);
    gl_Position = vec4(p3, 1.0);
    fPos = p3;

    // TBN from the quad's own 4 consecutive corners, projected to 3D. The quad's UVs are axis-aligned
    // ((0,0),(1,0),(1,1),(0,1)), so corner0->corner1 is +u (tangent), corner0->corner3 is +v (bitangent)
    // and the normal is their cross. A face whose 2D plane lies in the dropped ND dimensions projects
    // to zero area -> fall back to a viewer-facing frame.
    int c0 = gl_VertexID & ~3;                              // first corner of this quad (= 4 * face)
    vec3 e1 = project(c0 + 1, inst) - project(c0, inst);    // +u
    vec3 e2 = project(c0 + 3, inst) - project(c0, inst);    // +v
    vec3 nrm = cross(e1, e2);
    if (length(nrm) > 1e-6)
    {
        fTangent   = normalize(e1);
        fBitangent = normalize(e2);
        fNormal    = normalize(nrm);
    }
    else
    {
        fTangent   = vec3(1.0, 0.0, 0.0);
        fBitangent = vec3(0.0, 1.0, 0.0);
        fNormal    = vec3(0.0, 0.0, 1.0);
    }

    vec4 mat = materials[c0 >> 2];                          // one material record per base face
    fMatSpec = mat.rgb;
    fShininess = mat.w;
    fAlpha = world[inst * STRIDE + N * N + N];              // per-instance transparency

    fUv = uv;
    fCol = color;
}
