#version 450

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 TexCoord;
layout(location = 2) in vec3 Normal;

layout(set = 0, binding = 0) uniform BackgroundUniforms
{
    mat4 MVP;
};

layout(location = 0) out vec3 fsin_Normal;

void main()
{
    fsin_Normal = Normal; // Pass through normal (always pointing up for flat plane)
    gl_Position = MVP * vec4(Position, 1.0);
}
