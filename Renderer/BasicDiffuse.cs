using System;
using System.IO;
using System.Numerics;

namespace Kuro.Renderer {
    class BasicDiffuseShader : Shader {
        private static Texture2D _defaultTexture;

        public Matrix4x4 ViewProjection {
            set {
                SetUniform("uViewProjectionMatrix", value);
                GraphicsRenderer.AssertGLError();
            }
        }

        public Matrix4x4 World {
            set {
                SetUniform("uWorldMatrix", value);
                GraphicsRenderer.AssertGLError();

                // Matrix4x4.Invert(value, out var inverse);
                // SetUniform("uInverseWorldMatrix", inverse);
                // GraphicsRenderer.AssertGLError();
            }
        }

        // public Texture2D Texture {
        //     set {
        //         SetUniform("uTexture", value);
        //         GraphicsRenderer.AssertGLError();
        //     }
        // }
        public Texture2D Texture {get; set;} = _defaultTexture;

        public Vector4 Color {
            set {
                SetUniform("uColor", value);
                GraphicsRenderer.AssertGLError();
            }
        }

        static BasicDiffuseShader() {
            _defaultTexture = new Texture2D(stackalloc byte[] {0xff, 0xff, 0xff, 0xff}, 1, 1);
            _defaultTexture.WrapMode = TextureWrap.Clamp;
            WindowManager.Closing += () => _defaultTexture.Dispose();
        }

        public BasicDiffuseShader() : base(File.ReadAllText("Renderer/Shaders/BasicDiffuse.vert"), File.ReadAllText("Renderer/Shaders/BasicDiffuse.frag")) {
            Use();
        }

        public override void Use() {
            base.Use();
            SetUniform("uTexture", Texture);
        }
    }

    public class LightingInfo {
        
    }
}