#version 450

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 TexCoord;
layout(location = 2) in vec3 Normal;

layout(set = 0, binding = 0) uniform TerrainUniforms
{
    mat4 MVP;
    mat4 Model;
    vec3 LightDir;
    vec3 LightColor;
    vec3 AmbientColor;
    vec4 DiffuseColor;
    vec3 CameraPosition;
    float FadeStart;
    float FadeEnd;
};

layout(set = 1, binding = 0) uniform texture2D HeightmapTexture;
layout(set = 1, binding = 1) uniform sampler HeightmapSampler;

layout(location = 0) out vec2 fsin_TexCoord;
layout(location = 1) out vec3 fsin_Position;
layout(location = 2) out vec3 fsin_Normal;
layout(location = 3) out float fsin_Height;
layout(location = 4) out float fsin_FadeFactor;
layout(location = 5) out float fsin_DistanceFromCamera;

void main()
{
    // Sample heightmap (R16_UNorm gives 0.0 to 1.0)
    // Height formula: (normalized * 1000) + 500 = 500-1500m range
    float heightNorm = texture(sampler2D(HeightmapTexture, HeightmapSampler), TexCoord).r;
    float height = (heightNorm * 1000.0) + 500.0;

    // Calculate distance from camera for fog fade effect
    vec3 worldPos = vec3(Model * vec4(Position, 1.0));
    float distanceFromCamera = length(worldPos - CameraPosition);

    // Fog fade over distance range (from FadeStart to FadeEnd)
    float fadeFactor = 1.0; // - smoothstep(FadeStart, FadeEnd, distanceFromCamera);

    // Use full terrain height (no height blending)
    vec3 displacedPosition = Position;
    displacedPosition.y = height;

    // Calculate normal from neighboring heights
    // Step by ~1 meter in world space (tile is 500m wide, 513 vertices represent 500m)
    // Note: Heightmap is 513x513 pixels representing 500m (~0.977m per vertex)
    float texelSize = 1.0 / 500.0;
    vec2 uvL = TexCoord + vec2(-texelSize, 0.0);
    vec2 uvR = TexCoord + vec2(texelSize, 0.0);
    vec2 uvD = TexCoord + vec2(0.0, -texelSize);
    vec2 uvU = TexCoord + vec2(0.0, texelSize);

    float heightL = (texture(sampler2D(HeightmapTexture, HeightmapSampler), uvL).r * 1000.0) + 500.0;
    float heightR = (texture(sampler2D(HeightmapTexture, HeightmapSampler), uvR).r * 1000.0) + 500.0;
    float heightD = (texture(sampler2D(HeightmapTexture, HeightmapSampler), uvD).r * 1000.0) + 500.0;
    float heightU = (texture(sampler2D(HeightmapTexture, HeightmapSampler), uvU).r * 1000.0) + 500.0;

    vec3 tangentX = vec3(2.0, heightR - heightL, 0.0);
    vec3 tangentZ = vec3(0.0, heightU - heightD, 2.0);
    vec3 normal = normalize(cross(tangentZ, tangentX));

    fsin_Position = vec3(Model * vec4(displacedPosition, 1.0));
    fsin_Normal = normalize(vec3(Model * vec4(normal, 0.0)));
    fsin_TexCoord = TexCoord;
    fsin_Height = height; // Pass actual height for contour lines
    fsin_FadeFactor = fadeFactor;
    fsin_DistanceFromCamera = distanceFromCamera; // Pass distance for contour LOD

    gl_Position = MVP * vec4(displacedPosition, 1.0);
}
