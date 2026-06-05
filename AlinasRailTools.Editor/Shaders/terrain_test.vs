#version 330

// Input vertex attributes
layout(location = 0) in vec3 vertexPosition;
layout(location = 1) in vec2 vertexTexCoord;
layout(location = 2) in vec3 vertexNormal;

// Input uniform values
uniform mat4 mvp;
uniform mat4 matModel;
uniform sampler2D heightmap;

// Output vertex attributes (to fragment shader)
out vec2 fragTexCoord;
out vec3 fragPosition;
out vec3 fragNormal;
out float fragHeight; // Height for topographic lines

void main()
{
    // TEST: Simple sine wave pattern from 500 to 550m
    // Using UV coordinates to create a wave in both X and Z directions
    float waveX = sin(vertexTexCoord.x * 3.14159 * 4.0); // 4 waves across X
    float waveZ = sin(vertexTexCoord.y * 3.14159 * 4.0); // 4 waves across Z
    float height = 525.0 + (waveX + waveZ) * 12.5; // Range: 500-550m

    // Displace vertex position in Y direction
    vec3 displacedPosition = vertexPosition;
    displacedPosition.y = height;

    // Calculate normal by sampling neighboring wave heights
    float texelSize = 1.0 / 512.0; // One texel in UV space

    // Sample heights at neighboring positions
    vec2 uvL = vertexTexCoord + vec2(-texelSize, 0.0);
    vec2 uvR = vertexTexCoord + vec2(texelSize, 0.0);
    vec2 uvD = vertexTexCoord + vec2(0.0, -texelSize);
    vec2 uvU = vertexTexCoord + vec2(0.0, texelSize);

    float waveXL = sin(uvL.x * 3.14159 * 4.0);
    float waveZL = sin(uvL.y * 3.14159 * 4.0);
    float heightL = 525.0 + (waveXL + waveZL) * 12.5;

    float waveXR = sin(uvR.x * 3.14159 * 4.0);
    float waveZR = sin(uvR.y * 3.14159 * 4.0);
    float heightR = 525.0 + (waveXR + waveZR) * 12.5;

    float waveXD = sin(uvD.x * 3.14159 * 4.0);
    float waveZD = sin(uvD.y * 3.14159 * 4.0);
    float heightD = 525.0 + (waveXD + waveZD) * 12.5;

    float waveXU = sin(uvU.x * 3.14159 * 4.0);
    float waveZU = sin(uvU.y * 3.14159 * 4.0);
    float heightU = 525.0 + (waveXU + waveZU) * 12.5;

    // Calculate tangent vectors
    vec3 tangentX = vec3(2.0, heightR - heightL, 0.0);
    vec3 tangentZ = vec3(0.0, heightU - heightD, 2.0);

    // Normal is cross product of tangents
    vec3 normal = normalize(cross(tangentZ, tangentX));

    // Transform to world space
    fragPosition = vec3(matModel * vec4(displacedPosition, 1.0));
    fragNormal = normalize(vec3(matModel * vec4(normal, 0.0)));
    fragTexCoord = vertexTexCoord;
    fragHeight = height; // Pass height to fragment shader for contour lines

    // Calculate final vertex position
    gl_Position = mvp * vec4(displacedPosition, 1.0);
}
