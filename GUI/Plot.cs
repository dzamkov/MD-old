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
            this._LoadingZones = new LinkedList<_LoadingZone>();
            this._MainZone = this._Load(Data.Domain);
        }

        /// <summary>
        /// Renders the plot to the given render context.
        /// </summary>
        /// <param name="Size">The size of the renderable area.</param>
        /// <param name="Window">The area currently seen in the plot.</param>
        public void Render(GUIRenderContext Context, Point Size, Rectangle Window)
        {
            Context.PushClip(new Rectangle(Size));
            if(this._MainZone != null)
            {
                this._MainZone._RenderRecursive(Context, Window, Size, this._Multiplier, this._Gradient);
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
            // Update zones
            this._UpdateZone(this._MainZone, Window, Time);
        }

        private void _UpdateZone(PlotZone Zone, Rectangle Window, double Time)
        {
            if (!Zone._Loading)
            {
                const double FadeTime = 0.5;
                Zone._Fade = Math.Min(1.0, Zone._Fade + (Time / FadeTime));

                // Split if needed
                bool hassubzones = false;
                foreach (PlotZone sub in Zone.SubZones)
                {
                    hassubzones = true;
                    break;
                }
                if (!hassubzones)
                {
                    this._Split(Zone, 2, 2);
                }

                foreach (PlotZone subzone in Zone.SubZones)
                {
                    this._UpdateZone(subzone, Window, Time);
                }
            }
        }

        /// <summary>
        /// Splits a zone into the specified amount (horizontally and vertically) of subzones. The main
        /// zone is kept.
        /// </summary>
        private void _Split(PlotZone Zone, int X, int Y)
        {
            Rectangle area = Zone.Area;
            Point nsize = area.Size.Scale(new Point(1.0 / X, 1.0 / Y));
            int zones = X * Y;
            for (int x = 0; x < X; x++)
            {
                for (int y = 0; y < Y; y++)
                {
                    Rectangle rect = new Rectangle(area.Location + new Point(nsize.X * x, nsize.Y * y), nsize);
                    Zone._AddSubZone(this._Load(rect));
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
            this._MainZone._Delete();
        }

        /// <summary>
        /// Loads the specified area.
        /// </summary>
        internal PlotZone _Load(Rectangle Area)
        {
            PlotZone pz = PlotZone._CreateLoading(this._Data.GetZone(Area));
            lock (this)
            {
                this._LoadingZones.AddFirst(new _LoadingZone()
                {
                    Zone = pz
                });
                this._LoadPool.Signal();
            }
            return pz;
        }

        /// <summary>
        /// Gets the next task for the load thread pool.
        /// </summary>
        private Action _NextTask()
        {
            lock (this)
            {
                LinkedListNode<_LoadingZone> cur = this._LoadingZones.First;
                LinkedListNode<_LoadingZone> highest = null;
                while (cur != null)
                {
                    if (highest == null || cur.Value.Priority > highest.Value.Priority)
                    {
                        highest = cur;
                    }
                    cur = cur.Next;
                }
                if (highest != null)
                {
                    PlotZone toload = highest.Value.Zone;
                    this._LoadingZones.Remove(highest);
                    return toload._Load;
                }
            }
            return null;
        }

        private class _LoadingZone
        {
            /// <summary>
            /// The priority for this zone to be loaded.
            /// </summary>
            public double Priority;

            /// <summary>
            /// The zone to be loaded.
            /// </summary>
            public PlotZone Zone;
        }

        
        private PlotData _Data;
        private PlotZone _MainZone;
        private Gradient _Gradient;
        private double _Multiplier;
        private ThreadPool _LoadPool;
        private LinkedList<_LoadingZone> _LoadingZones;
    }

    /// <summary>
    /// A filled rectangular area shown in the Plot.
    /// </summary>
    public class PlotZone
    {
        private PlotZone()
        {
            this._SubZones = new LinkedList<PlotZone>();
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
        /// Gets the sub zones for this zone. Sub zones are rendered above the main zone and intersect at least part of
        /// the main zone's area. The order of subzones determines the order in which they are ordered after the main zone, with
        /// the last sub zone being the most visible.
        /// </summary>
        public IEnumerable<PlotZone> SubZones
        {
            get
            {
                return this._SubZones;
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
            foreach (PlotZone subzone in this._SubZones)
            {
                subzone._Delete();
            }
        }


        /// <summary>
        /// Renders the zone and subzones to the given context.
        /// </summary>
        internal void _RenderRecursive(GUIRenderContext Context, Rectangle Window, Point Size, double Multiplier, Gradient Gradient)
        {
            if (!this._Loading)
            {
                if (this._TextureNeedUpdate)
                {
                    this._UpdateTexture(Multiplier, Gradient);
                }
                if (this._Area.Intersects(Window))
                {
                    Rectangle rel = Window.ToRelative(this._Area);
                    rel.Location.Y = 1.0 - rel.Size.Y - rel.Location.Y;
                    rel = rel.Scale(Size);
                    Context.DrawTexture(this._Texture, Color.RGBA(1.0, 1.0, 1.0, this._Fade), rel);

                    foreach (PlotZone pz in this._SubZones)
                    {
                        pz._RenderRecursive(Context, Window, Size, Multiplier, Gradient);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a plot zone in the loading state.
        /// </summary>
        internal static PlotZone _CreateLoading(PlotData.Zone Source)
        {
            return new PlotZone()
            {
                _Loading = true,
                _Fade = 0.0,
                _Source = Source,
                _Area = Source.Area,
            };
        }

        /// <summary>
        /// Loads the data needed for this zone.
        /// </summary>
        internal void _Load()
        {
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

        /// <summary>
        /// Adds the specified zone as a sub zone above all others.
        /// </summary>
        internal void _AddSubZone(PlotZone SubZone)
        {
            this._SubZones.AddLast(SubZone);
        }

        internal bool _Loading;
        internal bool _TextureNeedUpdate;
        internal double _Fade;

        private double[] _Data;
        private int _Width;
        private int _Height;
        private int _Texture;
        
        private Rectangle _Area;
        private LinkedList<PlotZone> _SubZones;
        internal PlotData.Zone _Source;
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