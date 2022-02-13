#version 120

uniform sampler2D texture;

varying vec2 fTexCoords;

void main() {
    gl_FragColor = texture2D(texture, fTexCoords);
}