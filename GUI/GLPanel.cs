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
    /// Context given to a GLPanel when an event occurs.
    /// </summary>
    public interface IGLGUIContext
    {

    }

    /// <summary>
    /// The most basic unit of a gui system based on opengl.
    /// </summary>
    public abstract class GLPanel
    {
        /// <summary>
        /// Gets the size (in pixels) of this panel when rendered.
        /// </summary>
        public abstract Vector Size { get; }

        /// <summary>
        /// Renders the panel to the current GL context. The coordinate space for the panel
        /// should already be set up (with (0, 0) at the upperleft corner and (Size.X, Size.Y) at the
        /// bottomright corner).
        /// </summary>
        public abstract void Render();

        /// <summary>
        /// Updates the state of the panel after the specified amount of time elapses.
        /// </summary>
        public abstract void Update(double Time, IGLGUIContext Context);
    }

    /// <summary>
    /// A function that forces a GLPanel to resize.
    /// </summary>
    public delegate void GLPanelResizeAction(Vector NewSize, IGLGUIContext Context);
}