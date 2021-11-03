using System;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl;
using Silk.NET.Input;

namespace Kuro.Renderer {
    public static class WindowManager {
        private static IWindow _window;

        #region Events
        public static event Action<double> Update {
            add => _window.Update += value;
            remove => _window.Update -= value;
        }

        public static event Action<double> Render {
            add => _window.Render += value;
            remove => _window.Render -= value;
        }

        public static event Action<string[]> FileDrop {
            add => _window.FileDrop += value;
            remove => _window.FileDrop -= value;
        }

        public static event Action Closing {
            add => _window.Closing += value;
            remove => _window.Closing -= value;
        }
        #endregion Events

        public static void InitializeWindow(Action OnLoad) {
            var options = new WindowOptions {
                Title = "Kuro Level Editor",

                WindowState = WindowState.Maximized,

                API = new GraphicsAPI(ContextAPI.OpenGL, new APIVersion(2, 1)),
                PreferredStencilBufferBits = 8,
            };

            Window.PrioritizeSdl();
            _window = Window.Create(options);

            SdlWindowing.GetExistingApi(_window).GLSetAttribute(Silk.NET.SDL.GLattr.GLStencilSize, 8);

            _window.Load += () => {
                GraphicsRenderer.InitializeGraphics(GL.GetApi(_window));

                OnLoad?.Invoke();
            };

            _window.Run();
        }

        public static void Close() {
            _window.Close();
        }
    }
}