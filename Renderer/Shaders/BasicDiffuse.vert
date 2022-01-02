#version 120

attribute vec3 vPosition;
// attribute vec3 vNormal;
attribute vec2 vTexCoords;
// attribute vec3 vTangent;

uniform mat4 uWorldMatrix;
uniform mat4 uInverseWorldMatrix;
uniform mat4 uViewProjectionMatrix;

varying vec3 fPosition;
// varying vec3 fNormal;
varying vec2 fTexCoords;

void main() {
    gl_Position = uViewProjectionMatrix * uWorldMatrix * vec4(vPosition, 1.0);

    fPosition = vec3(uWorldMatrix * vec4(vPosition, 1.0));
    // fNormal = mat3(transpose(uInverseWorldMatrix)) * vNormal;
    fTexCoords = vTexCoords;
}