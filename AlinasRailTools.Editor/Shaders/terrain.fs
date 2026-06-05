#version 450

layout(location = 0) in vec2 fsin_TexCoord;
layout(location = 1) in vec3 fsin_Position;
layout(location = 2) in vec3 fsin_Normal;
layout(location = 3) in float fsin_Height;
layout(location = 4) in float fsin_FadeFactor;
layout(location = 5) in float fsin_DistanceFromCamera;

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

layout(location = 0) out vec4 fsout_Color;

void main()
{
    // Brown terrain color
    vec3 baseColor = vec3(0.6, 0.4, 0.2);

    // Simple diffuse lighting
    vec3 lightDir = normalize(vec3(0.5, -1.0, 0.3));
    vec3 normal = normalize(fsin_Normal);
    float diff = max(dot(normal, -lightDir), 0.0);

    // Combine ambient and diffuse
    vec3 ambient = baseColor * 0.3;
    vec3 diffuse = baseColor * diff;
    vec3 color = ambient + diffuse;

    // Static contour lines
    float height = fsin_Height;

    // 10m contour lines (medium detail)
    float mod10 = mod(height, 10.0);
    float thresh10 = 0.5;
    if (mod10 < thresh10 || mod10 > 10.0 - thresh10) {
        color = mix(color, vec3(0.15, 0.15, 0.15), 0.5 * fsin_FadeFactor);
    }

    // 100m contour lines (coarse detail, darker/thicker)
    float mod100 = mod(height, 100.0);
    float thresh100 = 1.0;
    if (mod100 < thresh100 || mod100 > 100.0 - thresh100) {
        color = mix(color, vec3(0.08, 0.08, 0.08), 0.7 * fsin_FadeFactor);
    }

    // Apply distance fog - fade terrain color towards sky blue based on distance
    vec3 skyColor = vec3(0.53, 0.81, 0.92); // Match clear color from Program.cs
    color = mix(skyColor, color, fsin_FadeFactor);

    // Output with full opacity (fog fade is in color, not alpha)
    fsout_Color = vec4(color, 1.0);
}
