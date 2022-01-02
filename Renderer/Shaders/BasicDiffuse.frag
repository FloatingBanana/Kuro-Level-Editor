#version 120

uniform sampler2D uTexture;
uniform vec4 uColor;

varying vec3 fPosition;
// varying vec3 fNormal;
varying vec2 fTexCoords;

void main() {
    gl_FragColor = uColor * texture2D(uTexture, fTexCoords);
}