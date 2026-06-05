#version 450

// Common fragment shader for solid color rendering
// Accepts interpolated color from vertex shader and outputs it

layout(location = 0) in vec3 fsin_Color;
layout(location = 0) out vec4 fsout_Color;

void main()
{
    fsout_Color = vec4(fsin_Color, 1.0);
}
