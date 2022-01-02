using System;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl;
using Silk.NET.Input;
using System.Linq;
using System.Numerics;

using SilkMath = Silk.NET.Maths;

namespace Kuro.Renderer {

    // REVIEW: I think this should be an object
    //         instead of a static class
    public static class WindowManager {
        private static IWindow _window;

        public static IKeyboard Keyboard {get; private set;}
        public static IMouse Mouse {get; private set;}

        public static Vector2 WindowSize {
            get => (Vector2)_window.Size;
            set => _window.Size = new SilkMath.Vector2D<int>((int)value.X, (int)value.Y);
        }

        public static Vector2 DisplaySize => (Vector2)_window.FramebufferSize;

        #region Events
        public static event Action Load;
        public static event Action<double> Update;
        public static event Action<double> Render;
        public static event Action<Vector2> Resize;
        public static event Action<string[]> FileDrop;
        public static event Action Closing;

        public static event Action<IKeyboard, Key, int> KeyDown;
        public static event Action<IKeyboard, Key, int> KeyUp;
        public static event Action<IKeyboard, char> KeyChar;

        public static event Action<IMouse, MouseButton, Vector2> MouseClick;
        public static event Action<IMouse, MouseButton, Vector2> MouseDoubleClick;
        public static event Action<IMouse, Vector2> MouseMove;
        #endregion Events

        public static void InitializeWindow() {
            var options = WindowOptions.Default;
            options.Title = "Kuro Level Editor";
            options.WindowState = WindowState.Maximized;
            options.API = new GraphicsAPI(ContextAPI.OpenGL, new APIVersion(2, 1));
            options.PreferredStencilBufferBits = 8;
            options.VSync = false;

            Window.PrioritizeSdl();
            _window = Window.Create(options);

            _window.Load += () => {
                GraphicsRenderer.InitializeGraphics(GL.GetApi(_window));

                IInputContext input = _window.CreateInput();

                Keyboard = input.Keyboards.FirstOrDefault();
                Keyboard.KeyDown += (keyboard, key, presses) => KeyDown?.Invoke(keyboard, key, presses);
                Keyboard.KeyUp   += (keyboard, key, presses) => KeyUp?.Invoke(keyboard, key, presses);
                Keyboard.KeyChar += (keyboard, character)    => KeyChar?.Invoke(keyboard, character);

                Mouse = input.Mice.FirstOrDefault();
                Mouse.Click += (mouse, button, pos) => MouseClick?.Invoke(mouse, button, pos);
                Mouse.DoubleClick += (mouse, button, pos) => MouseDoubleClick?.Invoke(mouse, button, pos);
                Mouse.MouseMove += (mouse, pos) => MouseMove?.Invoke(mouse, pos);

                Load?.Invoke();
            };

            _window.Update   += (delta) => Update?.Invoke(delta);
            _window.Render   += (delta) => Render?.Invoke(delta);
            _window.FileDrop += (files) => FileDrop?.Invoke(files);
            _window.Closing  += ()      => Closing?.Invoke();

            _window.Resize   += (size) => {
                GraphicsRenderer.gl.Viewport(size);

                Resize?.Invoke((Vector2)size);
            };

            _window.Run();
        }

        public static void Close() {
            _window.Close();
        }
    }
}