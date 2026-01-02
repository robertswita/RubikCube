#version 430 core
layout(location = 0) in vec3 vertPos;
layout(location = 1) in vec2 vertTexCoord;
layout(location = 2) in vec4 vertBones;
layout(location = 3) in vec4 vertWeights;
layout(location = 4) in vec3 vertNormal;
layout(location = 5) in vec3 vertTangent;
layout(location = 6) in vec3 vertBitangent;

layout(std140, binding = 0) uniform Camera {
    mat4 ViewProjectionMatrix;
    vec3 CameraPos;
};
//layout(std430, binding = 1) buffer Bones
const int MAX_BONES_TRANSFORMS = 100;
layout(std140, binding = 1) uniform Bones
{
    mat4 BonesTransforms[MAX_BONES_TRANSFORMS];
};
layout(location = 0) uniform mat4 ModelMatrix;

out vec3 fragPos;
out vec2 fragTexCoord;
out vec3 fragNormal;
out vec3 fragTangent;
out vec3 fragBitangent;
 
void main()
{
    vec4 pos = vec4(vertPos, 1);
    mat3 normalMatrix = mat3(ModelMatrix);
    if (vertWeights[0] > 0)
    {
        mat4 skinMatrix = mat4(0);
        for (int i = 0; i < 4; i++)
        {
            int boneIdx = int(vertBones[i]);
            float weight = vertWeights[i];
            if (weight > 0)
                skinMatrix += BonesTransforms[boneIdx] * weight;
        }
        pos = skinMatrix * pos;
        normalMatrix = mat3(skinMatrix);
    }
    else
        pos = ModelMatrix * pos;
    gl_Position = ViewProjectionMatrix * pos;
    fragTexCoord = vertTexCoord;
    fragPos = vec3(pos);
    fragNormal = transpose(inverse(normalMatrix)) * vertNormal;
    fragTangent = normalMatrix * vertTangent;
    fragBitangent = normalMatrix * vertBitangent;
}