#version 120

attribute vec2 vPos;
attribute vec2 vTexCoords;

varying vec2 fTexCoords;

void main() {
    gl_Position = vec4(vPos, 0.0, 1.0);
    fTexCoords = vTexCoords;
}