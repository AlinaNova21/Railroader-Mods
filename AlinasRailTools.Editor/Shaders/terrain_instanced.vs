#version 450

// Per-vertex attributes (from shared terrain mesh)
layout(location = 0) in vec3 Position;

// Per-instance attributes (tile world position offset)
layout(location = 1) in vec2 InstanceTilePosition;

// Uniforms
layout(set = 0, binding = 0) uniform TerrainUniforms
{
    mat4 ViewProjection;
    float TileDimension;
    float HeightScale;
    float HeightOffset;
};

layout(set = 1, binding = 0) uniform texture2D HeightmapTexture;
layout(set = 1, binding = 1) uniform sampler HeightmapSampler;

layout(location = 0) out vec2 fsin_TexCoord;
layout(location = 1) out vec3 fsin_Position;
layout(location = 2) out vec3 fsin_Normal;
layout(location = 3) out float fsin_Height;

void main()
{
    // Position is normalized (0-1) for the tile mesh
    // InstanceTilePosition provides world offset for this tile
    vec3 worldPos = vec3(
        InstanceTilePosition.x + Position.x * TileDimension,
        Position.y,  // Y will be replaced by heightmap
        InstanceTilePosition.y + Position.z * TileDimension
    );

    // Use normalized position as texture coordinate
    vec2 texCoord = Position.xz;

    // Sample heightmap (R16_UNorm gives 0.0 to 1.0)
    // Height formula: (normalized * HeightScale) + HeightOffset
    float heightNorm = texture(sampler2D(HeightmapTexture, HeightmapSampler), texCoord).r;
    float height = (heightNorm * HeightScale) + HeightOffset;

    // Apply height displacement
    worldPos.y = height;

    // Calculate normal from neighboring heights
    float texelSize = 1.0 / TileDimension;
    vec2 uvL = texCoord + vec2(-texelSize, 0.0);
    vec2 uvR = texCoord + vec2(texelSize, 0.0);
    vec2 uvD = texCoord + vec2(0.0, -texelSize);
    vec2 uvU = texCoord + vec2(0.0, texelSize);

    float heightL = (texture(sampler2D(HeightmapTexture, HeightmapSampler), uvL).r * HeightScale) + HeightOffset;
    float heightR = (texture(sampler2D(HeightmapTexture, HeightmapSampler), uvR).r * HeightScale) + HeightOffset;
    float heightD = (texture(sampler2D(HeightmapTexture, HeightmapSampler), uvD).r * HeightScale) + HeightOffset;
    float heightU = (texture(sampler2D(HeightmapTexture, HeightmapSampler), uvU).r * HeightScale) + HeightOffset;

    vec3 tangentX = vec3(2.0, heightR - heightL, 0.0);
    vec3 tangentZ = vec3(0.0, heightU - heightD, 2.0);
    vec3 normal = normalize(cross(tangentZ, tangentX));

    // Apply coordinate conversion (Unity to Veldrid: flip Z)
    mat4 coordConversion = mat4(
        1.0, 0.0, 0.0, 0.0,
        0.0, 1.0, 0.0, 0.0,
        0.0, 0.0, -1.0, 0.0,
        0.0, 0.0, 0.0, 1.0
    );

    vec4 veldridPos = coordConversion * vec4(worldPos, 1.0);
    gl_Position = ViewProjection * veldridPos;

    fsin_TexCoord = texCoord;
    fsin_Position = worldPos;
    fsin_Normal = normal;
    fsin_Height = height;
}
