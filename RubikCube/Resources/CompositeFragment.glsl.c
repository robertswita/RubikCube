#version 430
// WBOIT resolve: combine the transparent accumulation/revealage over the opaque scene color.
//   avg = accum.rgb / accum.a                 (weighted average transparent color)
//   final = opaque * reveal + avg * (1 - reveal)
in vec2 vUv;
layout(binding = 0) uniform sampler2D OpaqueTex;
layout(binding = 1) uniform sampler2D AccumTex;
layout(binding = 2) uniform sampler2D RevealTex;
out vec4 outColor;
void main()
{
    vec3 opaque = texture(OpaqueTex, vUv).rgb;
    vec4 accum = texture(AccumTex, vUv);
    float reveal = texture(RevealTex, vUv).r;
    vec3 avg = accum.rgb / max(accum.a, 1e-5);
    outColor = vec4(opaque * reveal + avg * (1.0 - reveal), 1.0);
}
