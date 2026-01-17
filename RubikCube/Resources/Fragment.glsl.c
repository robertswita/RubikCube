#version 430 core
in vec2 fragTexCoord;
in vec3 fragPos;
in vec3 fragNormal;
in vec3 fragTangent;
in vec3 fragBitangent;

layout (std140, binding = 0) uniform Camera {
    mat4 ViewProjectionMatrix;
    vec3 CameraPos;
};
struct Light {
    vec4 Pos;    
    vec3 Ambient;
    vec3 Diffuse;
    vec3 Specular;
    vec3 AttCoeff;
};
const int MAX_LIGHTS = 10;
layout (std140, binding = 2) uniform Lights {
    Light lights[MAX_LIGHTS];
    float LightsCount;
};
struct Material { 
    sampler2D DiffuseMap;
    sampler2D SpecularMap;
    sampler2D NormalMap;
    float Shininess;
};
layout (location = 1) uniform Material material;

out vec4 Color;

void main()
{    
    vec4 matDiffuse = texture(material.DiffuseMap, fragTexCoord);
    vec3 matSpecular = texture(material.SpecularMap, fragTexCoord).rgb;
    vec3 normal = 2 * texture(material.NormalMap, fragTexCoord).rgb - 1;
    vec3 T = normalize(fragTangent);
    vec3 B = normalize(fragBitangent);
    vec3 N = normalize(fragNormal);
    mat3 normalMatrix = mat3(T, B, N);
    normal = normalize(normalMatrix * normal);
    for (int i = 0; i < LightsCount; i++)
    {
        Light light = lights[i];
        vec3 lightDir = light.Pos.xyz;
        float attenuation = 1;
        if (light.Pos.w > 0)
        {
            lightDir -= fragPos;
	        float d = length(lightDir);
            lightDir /= d;
            attenuation = light.AttCoeff.x + light.AttCoeff.y * d + light.AttCoeff.z * d * d;
        }
	    vec3 ambient = light.Ambient * matDiffuse.rgb;
	    float cosAlpha = max(dot(lightDir, normal), 0.0f);
	    vec3 diffuse = light.Diffuse * matDiffuse.rgb * cosAlpha;
	    vec3 r = reflect(-lightDir, normal);
	    vec3 cameraDir = normalize(CameraPos - fragPos);
	    float cosTheta = max(dot(r, cameraDir), 0.0f);
	    vec3 specular = light.Specular * matSpecular * pow(cosTheta, material.Shininess);
    
	    Color.rgb += ambient + (diffuse + specular) / attenuation;
    }
    Color.a = matDiffuse.a;
}