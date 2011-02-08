using System;
using System.Collections.Generic;

using OpenTKGUI;

namespace MD.GUI
{
    /// <summary>
    /// An interactive visual representation of two-dimensional data.
    /// </summary>
    public abstract class View : Control
    {
        /// <summary>
        /// Gets bounds of the area that can be viewed.
        /// </summary>
        public abstract Rectangle Domain { get; }

        /// <summary>
        /// Gets or sets the rectangle containing the data that is currently seen.
        /// </summary>
        public Rectangle Window
        {
            get
            {
                return this._Window;
            }
            set
            {
                this._Window = value;
            }
        }

        public override void Update(GUIControlContext Context, double Time)
        {
            MouseState ms = Context.MouseState;
            if (ms != null)
            {
                // Zoom and stuff
                double scroll = ms.Scroll;
                if (scroll != 0.0)
                {
                    double zoom = Math.Pow(2.0, -scroll / 40.0);
                    Rectangle win = this._Window;
                    Point mousepos = new Rectangle(this.Size).ToRelative(ms.Position);
                    mousepos.Y = 1.0 - mousepos.Y;
                    Point nwinsize = win.Size * zoom;
                    Point nwinpos = win.Location + mousepos.Scale(win.Size) - mousepos.Scale(nwinsize);
                    this._Window = new Rectangle(nwinpos, nwinsize).Intersection(this.Domain);
                }
            }
        }

        private Rectangle _Window;
    }
}