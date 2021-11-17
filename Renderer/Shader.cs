using System;
using System.Numerics;
using System.IO;
using Silk.NET.OpenGL.Legacy;

namespace Kuro.Renderer {
    public class Shader : IDisposable {
        private static GL _gl => GraphicsRenderer.gl;
        private readonly uint _handle;

        public Shader(string vertShaderCode, string fragShaderCode) {
            uint vertex = LoadShader(ShaderType.VertexShader, vertShaderCode);
            uint fragment = LoadShader(ShaderType.FragmentShader, fragShaderCode);

            _handle = _gl.CreateProgram();
            _gl.AttachShader(_handle, vertex);
            _gl.AttachShader(_handle, fragment);
            _gl.LinkProgram(_handle);

            _gl.GetProgram(_handle, GLEnum.LinkStatus, out var status);
            if (status == 0) {
                throw new Exception($"Failed to link: {_gl.GetProgramInfoLog(_handle)}");
            }

            _gl.DetachShader(_handle, vertex);
            _gl.DetachShader(_handle, fragment);
            _gl.DeleteShader(vertex);
            _gl.DeleteShader(fragment);
        }

        public void Use() {
            _gl.UseProgram(_handle);
        }

        public int GetUniformLocation(string name) {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1) {
                throw new Exception($"'{name}' uniform not found on shader");
            }
            return location;
        }

        public void SetUniform(string name, int value) {
            _gl.Uniform1(GetUniformLocation(name), value);
        }

        public void SetUniform(string name, float value) {
            _gl.Uniform1(GetUniformLocation(name), value);
        }

        public unsafe void SetUniform(string name, Matrix4x4 value) {
            _gl.UniformMatrix4(GetUniformLocation(name), 1, false, (float*)&value);
        }

        public void SetUniform(string name, Vector3 value) {
            _gl.Uniform3(GetUniformLocation(name), value);
        }

        public int GetAttribLocation(string name) {
            return _gl.GetAttribLocation(_handle, name);
        }

        private static uint LoadShader(ShaderType type, string code) {
            uint handle = _gl.CreateShader(type);

            _gl.ShaderSource(handle, code);
            _gl.CompileShader(handle);

            _gl.GetShader(handle, GLEnum.CompileStatus, out var status);
            if (status == 0) {
                throw new Exception($"Error compiling shader of type {type}: {_gl.GetShaderInfoLog(handle)}");
            }

            return handle;
        }

        public void Dispose() {
            _gl.DeleteProgram(_handle);
        }

        public static Shader CreateFromPath(string fragShaderPath, string vertShaderPath) {
            return new Shader(File.ReadAllText(fragShaderPath), File.ReadAllText(vertShaderPath));
        }
    }
}