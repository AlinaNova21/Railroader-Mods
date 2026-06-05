#version 450

layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 VertexColor;

layout(set = 0, binding = 0) uniform NodeUniforms
{
    mat4 MVP;
    vec4 Color;
};

layout(location = 0) out vec3 fsin_Color;

void main()
{
    fsin_Color = Color.rgb;  // Use the uniform color, not vertex color
    gl_Position = MVP * vec4(Position, 1.0);
}
