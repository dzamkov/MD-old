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
    /// Handles textures and resources needed to create a visual representation of data that shows the correlation 
    /// between two independant variables and a dependant variable.
    /// </summary>
    public class Plot : IDisposable
    {
        public Plot(PlotData Data)
        {
            this._Data = Data;

            this._Gradient = new Gradient(
                Color.RGB(1.0, 1.0, 1.0),
                Color.RGB(0.5, 0.0, 0.0),
                new Gradient.Stop[]
                {
                    new Gradient.Stop(Color.RGB(0.0, 1.0, 1.0), 0.35),
                    new Gradient.Stop(Color.RGB(0.0, 1.0, 0.0), 0.5),
                    new Gradient.Stop(Color.RGB(1.0, 1.0, 0.0), 0.6),
                    new Gradient.Stop(Color.RGB(1.0, 0.0, 0.0), 0.85),
                });
            this._Multiplier = 400.0;
            this._LoadPool = new ThreadPool(this._NextTask);
            this._LoadPool.TargetThreadAmount = 3;

            Rectangle domain = Data.Domain;
            this._LastWindow = domain;
            this._MainNode = PlotNode._CreateLoading(Data.GetZone(domain));
        }

        /// <summary>
        /// Renders the plot to the given render context.
        /// </summary>
        /// <param name="Size">The size of the renderable area.</param>
        /// <param name="Window">The area currently seen in the plot.</param>
        public void Render(GUIRenderContext Context, Point Size, Rectangle Window)
        {
            Context.PushClip(new Rectangle(Size));
            if(this._MainNode != null)
            {
                this._MainNode._RenderRecursive(Context, Window, Size, this._Multiplier, this._Gradient);
            }
            Context.Pop();
        }

        /// <summary>
        /// Updates the plot by the given amount of time in seconds. This must be called on the thread
        /// with the GL context used by render, as textures may need to be created.
        /// </summary>
        /// <param name="Window">The area currently seen in the plot.</param>
        public void Update(Rectangle Window, double Time)
        {
            this._LastWindow = Window;

            // Update zones
            this._UpdateNode(this._MainNode, Window, Time);

            // Handle signalling for thread pool
            this._SignalTime--;
            if (this._SignalTime < 0)
            {
                this._SignalTime = 100;
                this._LoadPool.Signal();
            }
        }

        private void _UpdateNode(PlotNode Node, Rectangle Window, double Time)
        {
            if (!Node._Loading)
            {
                const double FadeTime = 0.5;
                Node._Fade = Math.Min(1.0, Node._Fade + (Time / FadeTime));

                foreach (PlotNode subnode in Node.SubNodes)
                {
                    this._UpdateNode(subnode, Window, Time);
                }
            }
        }

        /// <summary>
        /// Gets the gradient used for displaying data.
        /// </summary>
        public Gradient Gradient
        {
            get
            {
                return this._Gradient;
            }
        }

        /// <summary>
        /// Gets how much the source data is multiplied before the gradient is applied.
        /// </summary>
        public double Multiplier
        {
            get
            {
                return this._Multiplier;
            }
        }

        /// <summary>
        /// Gets the data for the plot.
        /// </summary>
        public PlotData Data
        {
            get
            {
                return this._Data;
            }
        }

        public void Dispose()
        {
            this._MainNode._Delete();
        }

        /// <summary>
        /// Gets the next task for the load thread pool.
        /// </summary>
        private Action _NextTask()
        {
            double priority;
            PlotNode next;
            lock (this)
            {
                next = this._MainNode._NextLoad(this._LastWindow, out priority);
            }
            if (next != null && priority > 0.1)
            {
                return delegate
                {
                    lock (this)
                    {
                        next._NeedLoad = false;
                        next._Split(this._Data);
                    }
                    next._Load();
                };
            }
            else
            {
                return null;
            }
        }

        private int _SignalTime;
        private Rectangle _LastWindow;
        private PlotData _Data;
        private PlotNode _MainNode;
        private Gradient _Gradient;
        private double _Multiplier;
        private ThreadPool _LoadPool;
    }

    /// <summary>
    /// A filled rectangular area shown in the Plot. May have subnodes that together cover the node's full area.
    /// </summary>
    public class PlotNode
    {
        private PlotNode()
        {

        }

        /// <summary>
        /// Gets the area this zone is for.
        /// </summary>
        public Rectangle Area
        {
            get
            {
                return this._Area;
            }
        }

        /// <summary>
        /// Gets if this node is currently loading.
        /// </summary>
        public bool Loading
        {
            get
            {
                return this._Loading;
            }
        }

        /// <summary>
        /// Gets the subnodes for this node, or null if they have yet to be created.
        /// </summary>
        public IEnumerable<PlotNode> SubNodes
        {
            get
            {
                return this._SubNodes;
            }
        }

        /// <summary>
        /// Deletes the plot zone and sub zones.
        /// </summary>
        internal void _Delete()
        {
            if (this._Texture > 0)
            {
                GL.DeleteTexture(this._Texture);
            }
            if (this._SubNodes != null)
            {
                foreach (PlotNode subnode in this._SubNodes)
                {
                    subnode._Delete();
                }
            }
        }

        /// <summary>
        /// Renders the node and subnodes to the given context.
        /// </summary>
        internal void _RenderRecursive(GUIRenderContext Context, Rectangle Window, Point Size, double Multiplier, Gradient Gradient)
        {
            if (!this._Loading)
            {
                if (this._Area.Intersects(Window))
                {
                    // Check if this node needs to be rendered (if all children are loaded, no).
                    bool needrender = false;
                    foreach (PlotNode pn in this._SubNodes)
                    {
                        if (pn._Loading || pn._Fade < 1.0)
                        {
                            needrender = true;
                        }
                    }

                    // Render and manage textures if needed
                    if (needrender)
                    {
                        if (this._TextureNeedUpdate)
                        {
                            this._UpdateTexture(Multiplier, Gradient);
                        }
                        Rectangle rel = Window.ToRelative(this._Area);
                        rel.Location.Y = 1.0 - rel.Size.Y - rel.Location.Y;
                        rel = rel.Scale(Size);
                        Context.DrawTexture(this._Texture, Color.RGBA(1.0, 1.0, 1.0, this._Fade), rel);
                    }
                    else
                    {
                        if (this._Texture > 0)
                        {
                            GL.DeleteTexture(this._Texture);
                            this._Texture = 0;
                            this._TextureNeedUpdate = true;
                        }
                    }
                    
                    // Render subnodes
                    foreach(PlotNode pn in this._SubNodes)
                    {
                        pn._RenderRecursive(Context, Window, Size, Multiplier, Gradient);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a plot zone in the loading state.
        /// </summary>
        internal static PlotNode _CreateLoading(PlotData.Zone Source)
        {
            return new PlotNode()
            {
                _Loading = true,
                _NeedLoad = true,
                _Fade = 0.0,
                _Source = Source,
                _Area = Source.Area,
            };
        }

        /// <summary>
        /// Gets the next descendant subnode of this (including this) to load based on visibility and intrest giving the last used window.
        /// </summary>
        /// <param name="Priority">The relative priority for the item to be loaded.</param>
        internal PlotNode _NextLoad(Rectangle Window, out double Priority)
        {
            Rectangle area = this._Area;
            if (Window.Intersects(area))
            {
                Rectangle intersection = area.Intersection(Window);
                double visibility = intersection.Size.X / Window.Size.X * intersection.Size.Y / Window.Size.Y;
                if (this._NeedLoad)
                {
                    Priority = visibility;
                    return this;
                }
                if (!this._Loading)
                {
                    PlotNode high = null;
                    Priority = 0.0;
                    foreach (PlotNode sn in this._SubNodes)
                    {
                        double snpriority;
                        PlotNode snnext = sn._NextLoad(Window, out snpriority);
                        if (snpriority > Priority)
                        {
                            high = snnext;
                            Priority = snpriority;
                        }
                    }
                    return high;
                }
            }
            Priority = 0.0;
            return null;
        }

        /// <summary>
        /// Loads the data needed for this zone.
        /// </summary>
        internal void _Load()
        {
            this._NeedLoad = false;
            double pixels = 96 * 96;
            double sw = Math.Sqrt(pixels * this._Source.SampleRatio);
            double sh = pixels / sw;
            int w = (int)sw;
            int h = (int)sh;
            this._Width = w;
            this._Height = h;
            _ArrayOutput ao = new _ArrayOutput(w, h, ref this._Data);
            this._Source.GetData<_ArrayOutput>(w, h, ao);
            this._TextureNeedUpdate = true;
            this._Loading = false;
        }

        /// <summary>
        /// Splits the node by creating subnodes.
        /// </summary>
        internal void _Split(PlotData SourceData)
        {
            List<double> XSplits = new List<double>(); XSplits.Add(0.0);
            List<double> YSplits = new List<double>(); YSplits.Add(0.0);
            this._Source.SuggestSplit(XSplits, YSplits);
            XSplits.Add(1.0);
            YSplits.Add(1.0);
            this._Split(SourceData, XSplits, YSplits);
        }

        /// <summary>
        /// Splits the node (by creating subnodes) along the given relative split-lines.
        /// </summary>
        private void _Split(PlotData SourceData, List<double> XSplits, List<double> YSplits)
        {
            Rectangle area = this._Area;

            // Make split lines absolute
            for (int t = 0; t < XSplits.Count; t++)
            {
                XSplits[t] = area.Location.X + XSplits[t] * area.Size.X;
            }
            for (int t = 0; t < YSplits.Count; t++)
            {
                YSplits[t] = area.Location.Y + YSplits[t] * area.Size.Y;
            }

            // Create sub nodes
            int wnodes = XSplits.Count - 1;
            int hnodes = YSplits.Count - 1;
            this._SubNodes = new List<PlotNode>(wnodes * hnodes);
            for (int x = 0; x < wnodes; x++)
            {
                for (int y = 0; y < hnodes; y++)
                {
                    Rectangle subrect = new Rectangle(XSplits[x], YSplits[y], XSplits[x + 1], YSplits[y + 1]);
                    subrect.Size -= subrect.Location;
                    this._SubNodes.Add(_CreateLoading(SourceData.GetZone(subrect)));
                }
            }
        }

        /// <summary>
        /// Plot data output to an array.
        /// </summary>
        private class _ArrayOutput : PlotData.Zone.IDataOutput
        {
            public _ArrayOutput(int Width, int Height, ref double[] Data)
            {
                this._Data = Data = new double[Width * Height];
                this._Width = Width;
            }

            public void Set(int X, int Y, double Value)
            {
                this._Data[X + Y * this._Width] = Value;
            }

            private int _Width;
            private double[] _Data;
        }

        /// <summary>
        /// Updates the texture for the zone.
        /// </summary>
        private void _UpdateTexture(double Multiplier, Gradient Gradient)
        {
            if (this._Texture > 0)
            {
                GL.DeleteTexture(this._Texture);
            }
            byte[] pdat = new byte[this._Width * this._Height * 4];
            for (int i = 0; i < this._Data.Length; i++)
            {
                double val = this._Data[i];
                val *= Multiplier;
                val = Math.Min(val, 1.0);
                Color col = Gradient.GetColor(val);
                byte r = (byte)(col.R * 255.0);
                byte g = (byte)(col.G * 255.0);
                byte b = (byte)(col.B * 255.0);
                pdat[i * 4 + 0] = b;
                pdat[i * 4 + 1] = g;
                pdat[i * 4 + 2] = r;
                pdat[i * 4 + 3] = 255;
            }

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, this._Width, this._Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, pdat);
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float)TextureEnvMode.Modulate);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureWrapMode.ClampToEdge);
            this._Texture = id;
            this._TextureNeedUpdate = false;
        }

        internal bool _NeedLoad;
        internal bool _Loading;
        internal bool _TextureNeedUpdate;
        internal double _Fade;

        private double[] _Data;
        private int _Width;
        private int _Height;
        private int _Texture;
        
        private Rectangle _Area;
        private List<PlotNode> _SubNodes;
        private PlotData.Zone _Source;
    }

    /// <summary>
    /// Provides data for a plot.
    /// </summary>
    public abstract class PlotData
    {
        /// <summary>
        /// Gets the bounds of the data that can be produced for the plot.
        /// </summary>
        public abstract Rectangle Domain { get; }

        /// <summary>
        /// Gets a zone in this data.
        /// </summary>
        public abstract Zone GetZone(Rectangle Area);

        /// <summary>
        /// A filled rectangle in the plot data.
        /// </summary>
        public abstract class Zone
        {
            /// <summary>
            /// Gets the area the zone occupies.
            /// </summary>
            public abstract Rectangle Area { get; }

            /// <summary>
            /// Gets the optimal ratio of width samples to height samples for the data.
            /// </summary>
            public abstract double SampleRatio { get; }

            /// <summary>
            /// If this zone were to be split into subzones that cover the total area, gets the relative
            /// lines along the zone that indicate where the splits should occur for optimal performance
            /// and visibility.
            /// </summary>
            public virtual void SuggestSplit(List<double> XSplits, List<double> YSplits)
            {
                XSplits.Add(0.5);
                YSplits.Add(0.5);
            }

            /// <summary>
            /// An interface that can accept the output from GetData.
            /// </summary>
            public interface IDataOutput
            {
                /// <summary>
                /// Sets a sample of data for the output.
                /// </summary
                void Set(int X, int Y, double Value);
            }

            /// <summary>
            /// Gets the data for the zone and sends it to the specified output.
            /// </summary>
            public abstract void GetData<TOutput>(int Width, int Height, TOutput Output)
                where TOutput : IDataOutput;
        }
    }
}