#version 450

// Per-vertex attributes
layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 VertexColor;

// Per-instance attributes (matrix stored as 4 vec4s, coming from C# row-major)
layout(location = 2) in vec4 InstanceTransform0;
layout(location = 3) in vec4 InstanceTransform1;
layout(location = 4) in vec4 InstanceTransform2;
layout(location = 5) in vec4 InstanceTransform3;

// Uniforms (view-projection only, model comes from instance data)
layout(set = 0, binding = 0) uniform TieUniforms
{
    mat4 ViewProjection;
    vec4 Color;
};

layout(location = 0) out vec3 fsin_Color;

void main()
{
    // Reconstruct instance transform matrix from vec4s - NO TRANSPOSE
    mat4 instanceTransform = mat4(
        InstanceTransform0,
        InstanceTransform1,
        InstanceTransform2,
        InstanceTransform3
    );

    // Calculate final position
    mat4 mvp = ViewProjection * instanceTransform;
    gl_Position = mvp * vec4(Position, 1.0);

    fsin_Color = Color.rgb;
}
