#version 450

layout(location = 0) in vec3 fsin_Normal;

layout(location = 0) out vec4 fsout_Color;

void main()
{
    // Brown terrain color (same as terrain tiles)
    vec3 baseColor = vec3(0.6, 0.4, 0.2);

    // Same lighting as terrain tiles
    vec3 lightDir = normalize(vec3(0.5, -1.0, 0.3));
    vec3 normal = normalize(fsin_Normal);
    float diff = max(dot(normal, -lightDir), 0.0);

    // Combine ambient and diffuse
    vec3 ambient = baseColor * 0.3;
    vec3 diffuse = baseColor * diff;
    vec3 color = ambient + diffuse;

    fsout_Color = vec4(color, 1.0);
}
