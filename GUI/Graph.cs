using System;
using System.Collections.Generic;
using System.Threading;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTKGUI;

namespace MD.GUI
{
    /// <summary>
    /// A visual representation of the correlation between two variables.
    /// </summary>
    public abstract class Graph : IDisposable
    {
        /// <summary>
        /// Gets the domain (viewable area) of the graph.
        /// </summary>
        public virtual Rectangle Domain
        {
            get
            {
                return new Rectangle(0.0, 0.0, double.PositiveInfinity, double.PositiveInfinity);
            }
        }

        /// <summary>
        /// Renders graph plot to the given render context.
        /// </summary>
        /// <param name="Size">The size of the renderable area.</param>
        /// <param name="Window">The area currently seen in the graph.</param>
        public virtual void Render(GUIRenderContext Context, Point Size, Rectangle Window)
        {

        }

        /// <summary>
        /// Updates the graph by the given amount of time in seconds. This must be called on the thread
        /// with the GL context used by render, as textures may need to be created.
        /// </summary>
        /// <param name="Window">The area currently seen in the graph.</param>
        public virtual void Update(Rectangle Window, double Time)
        {

        }


        public void Dispose()
        {
            this.OnDispose();
        }

        /// <summary>
        /// Called when the graph is removed and can deallocate resources.
        /// </summary>
        public virtual void OnDispose()
        {

        }
    }
}