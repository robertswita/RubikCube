#version 430
#include "Variables.glsl"
// Lighting pass shared by both the opaque and the transparent (WBOIT) passes. uOIT selects the output:
//   uOIT == 0 (opaque pass):      outColor0 = lit color, written to the scene's opaque target.
//   uOIT == 1 (transparent pass): outColor0 = weighted accumulation, outColor1 = revealage (McGuire-
//                                 Bavoil weighted-blended OIT), later composited over the opaque target.
in vec2 fUv;
in vec4 fCol;
in vec3 fPos;
flat in vec3 fNormal;
flat in vec3 fTangent;
flat in vec3 fBitangent;
flat in vec3 fMatSpec;
flat in float fShininess;
flat in float fAlpha;

layout(location = 0) out vec4 outColor0;
layout(location = 1) out vec4 outColor1;

layout(location = 1) uniform int uOIT;   // 0 = opaque pass, 1 = transparent (WBOIT) pass

// std140 light record. Each vec3 is padded to a vec4 so the C# host packing is trivial; the .w slots
// carry flags/counts. Position.w = isDirectional (then .xyz is the direction toward the light).
struct Light {
    vec4 Position;   // xyz world position (or direction); w = isDirectional
    vec4 Ambient;    // xyz
    vec4 Diffuse;    // xyz
    vec4 Specular;   // xyz
    vec4 Att;        // xyz = constant / linear / quadratic attenuation
};

layout(std140, binding = 10) uniform LightsBuffer {
    vec4 LightHeader;              // x = number of active lights
    Light Lights[MAX_LIGHTS];
};

// Material maps (texture units set by the layout(binding) qualifier). basecolor -> diffuse, normal ->
// tangent-space normal, roughness -> specular exponent, AO -> ambient. height (parallax) is deferred
// until there is a movable camera to make it worthwhile.
layout(binding = 0) uniform sampler2D DiffuseMap;
layout(binding = 1) uniform sampler2D NormalMap;
layout(binding = 2) uniform sampler2D ArrowMap;      // black arrow on white; a shape-agnostic orientation decal
layout(binding = 3) uniform sampler2D RoughnessMap;  // -> specular exponent (per-texel highlight sharpness)
layout(binding = 4) uniform sampler2D AOMap;         // ambient occlusion (modulates the ambient term only)

vec3 shade()
{
    int count = int(LightHeader.x);
    if (count == 0) return fCol.rgb;   // no lights -> flat unlit color

    // Orient the face toward the viewer (the ND->3D projection is orthographic, so the view direction
    // is a constant +Z). Not named N - that is the dimension macro (#define N ...) from Variables.
    vec3 nrm = fNormal;
    if (nrm.z < 0.0) nrm = -nrm;

    // TBN frame: sample the tangent-space normal from the normal map and rotate it into world space.
    mat3 TBN = mat3(normalize(fTangent), normalize(fBitangent), nrm);
    vec3 tangentNormal = texture(NormalMap, fUv).rgb * 2.0 - 1.0;
    vec3 Nw = normalize(TBN * tangentNormal);

    vec3 V = vec3(0.0, 0.0, 1.0);
    vec3 matDiffuse = fCol.rgb * texture(DiffuseMap, fUv).rgb;   // sticker color tinted by the basecolor map
    // Orientation arrow decal (shape lives in the image, not here): white -> keep the surface,
    // black -> complement of the sticker color, so the mark is visible on any sticker color.
    float arrow = 1.0 - texture(ArrowMap, fUv).r;
    matDiffuse = mix(matDiffuse, vec3(1.0) - fCol.rgb, arrow);
    vec3 matSpecular = fMatSpec;

    // Per-texel specular exponent from the roughness map (rough 0 -> sharp 256, rough 1 -> broad 1).
    float shininess = exp2((1.0 - texture(RoughnessMap, fUv).r) * 8.0);
    float ao = texture(AOMap, fUv).r;                           // darkens only the ambient (indirect) term

    vec3 lit = vec3(0.0);
    for (int i = 0; i < count; i++)
    {
        Light lgt = Lights[i];
        vec3 L;
        float att = 1.0;
        if (lgt.Position.w != 0.0)                 // directional
            L = normalize(lgt.Position.xyz);
        else
        {
            vec3 d = lgt.Position.xyz - fPos;
            float dist = length(d);
            L = d / max(dist, 1e-4);
            att = 1.0 / (lgt.Att.x + lgt.Att.y * dist + lgt.Att.z * dist * dist);
        }
        vec3 ambient = lgt.Ambient.xyz * matDiffuse * ao;
        float diff = max(dot(Nw, L), 0.0);
        vec3 diffuse = lgt.Diffuse.xyz * matDiffuse * diff;
        vec3 H = normalize(L + V);
        float spec = pow(max(dot(Nw, H), 0.0), shininess);
        vec3 specular = lgt.Specular.xyz * matSpecular * spec;
        lit += ambient + att * (diffuse + specular);
    }
    return lit;
}

void main()
{
    vec3 lit = shade();

    if (uOIT == 0)                       // opaque pass: straight color
    {
        outColor0 = vec4(lit, 1.0);
        return;
    }

    // Transparent pass (weighted-blended OIT). Accumulate premultiplied color * weight and, separately,
    // the revealage (product of (1 - alpha), realised by the GL_ZERO/ONE_MINUS_SRC_COLOR blend on target 1).
    float a = fAlpha;
    float z = gl_FragCoord.z;
    float w = a * clamp(0.03 / (1e-5 + pow(z, 4.0)), 1e-2, 3e3);
    outColor0 = vec4(lit * a, a) * w;    // accum   (blend GL_ONE, GL_ONE)
    outColor1 = vec4(a);                 // reveal  (blend GL_ZERO, GL_ONE_MINUS_SRC_COLOR -> reveal *= 1-a)
}
