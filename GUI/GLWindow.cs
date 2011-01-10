using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace MD.GUI
{
    /// <summary>
    /// A window hosting a GLPanel.
    /// </summary>
    public class GLWindow : GameWindow
    {
        public GLWindow(GLPanel Panel, GLPanelResizeAction Resize, string Title)
            : base(640, 480, GraphicsMode.Default, Title, GameWindowFlags.Default)
        {
            this.WindowState = WindowState.Maximized;
            this._Panel = Panel;
            this._Resize = Resize;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.ClearColor(Color.RGB(0.0, 0.0, 0.0));
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();

            GL.Scale(2.0, -2.0, 1.0);
            GL.Translate(-0.5, -0.5, 0.0);
            GL.Scale(1.0 / (double)this.Width, 1.0 / (double)this.Height, 1.0);

            GL.Begin(BeginMode.Quads);
            GL.Vertex2(0.0, 0.0);
            GL.Vertex2(100.0, 0.0);
            GL.Vertex2(100.0, 100.0);
            GL.Vertex2(0.0, 100.0);
            GL.End();

            this.SwapBuffers();
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, this.Width, this.Height);
        }

        private GLPanel _Panel;
        private GLPanelResizeAction _Resize;
    }
}