#version 430
// WBOIT resolve + FXAA. resolve(uv) reconstructs the final color at any texel (transparent average over
// the opaque background); FXAA then runs its edge-detect/blend over resolve() instead of a single texture,
// so anti-aliasing happens in this existing composite pass - no extra target, no multisampling.
in vec2 vUv;
layout(binding = 0) uniform sampler2D OpaqueTex;
layout(binding = 1) uniform sampler2D AccumTex;
layout(binding = 2) uniform sampler2D RevealTex;
out vec4 outColor;

#define FXAA_REDUCE_MIN (1.0 / 128.0)
#define FXAA_REDUCE_MUL (1.0 / 8.0)
#define FXAA_SPAN_MAX   8.0

// Weighted-blended OIT resolve: avg = accum.rgb / accum.a, final = opaque * reveal + avg * (1 - reveal).
vec3 resolve(vec2 uv)
{
    vec3 opaque = texture(OpaqueTex, uv).rgb;
    vec4 accum = texture(AccumTex, uv);
    float reveal = texture(RevealTex, uv).r;
    vec3 avg = accum.rgb / max(accum.a, 1e-5);
    return opaque * reveal + avg * (1.0 - reveal);
}

float luma(vec3 c) { return dot(c, vec3(0.299, 0.587, 0.114)); }

void main()
{
    vec2 texel = 1.0 / vec2(textureSize(OpaqueTex, 0));

    vec3 rgbM  = resolve(vUv);
    vec3 rgbNW = resolve(vUv + vec2(-1.0, -1.0) * texel);
    vec3 rgbNE = resolve(vUv + vec2( 1.0, -1.0) * texel);
    vec3 rgbSW = resolve(vUv + vec2(-1.0,  1.0) * texel);
    vec3 rgbSE = resolve(vUv + vec2( 1.0,  1.0) * texel);

    float lM = luma(rgbM), lNW = luma(rgbNW), lNE = luma(rgbNE), lSW = luma(rgbSW), lSE = luma(rgbSE);
    float lMin = min(lM, min(min(lNW, lNE), min(lSW, lSE)));
    float lMax = max(lM, max(max(lNW, lNE), max(lSW, lSE)));

    // Edge direction (perpendicular to the luma gradient).
    vec2 dir;
    dir.x = -((lNW + lNE) - (lSW + lSE));
    dir.y =  ((lNW + lSW) - (lNE + lSE));
    float reduce = max((lNW + lNE + lSW + lSE) * (0.25 * FXAA_REDUCE_MUL), FXAA_REDUCE_MIN);
    float rcpMin = 1.0 / (min(abs(dir.x), abs(dir.y)) + reduce);
    dir = clamp(dir * rcpMin, -FXAA_SPAN_MAX, FXAA_SPAN_MAX) * texel;

    // Two taps near the center, two wider taps; keep the wider blend unless it over/undershoots the range.
    vec3 rgbA = 0.5 * (resolve(vUv + dir * (1.0 / 3.0 - 0.5)) + resolve(vUv + dir * (2.0 / 3.0 - 0.5)));
    vec3 rgbB = rgbA * 0.5 + 0.25 * (resolve(vUv + dir * -0.5) + resolve(vUv + dir * 0.5));
    float lB = luma(rgbB);

    vec3 result = (lB < lMin || lB > lMax) ? rgbA : rgbB;
    outColor = vec4(result, 1.0);
}
