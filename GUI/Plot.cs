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
    /// A control that shows the correlation between two independant variables and a dependant variable.
    /// </summary>
    public class Plot : Control
    {
        public Plot()
        {
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
            this._ThreadPool = new ThreadPool();
            this._ThreadPool.TargetThreadAmount = 1;
            this._ActionQueue = new List<Action>();
        }

        public override void Render(GUIRenderContext Context)
        {
            Point size = this.Size;
            Context.PushClip(new Rectangle(size));
            Rectangle win = this._Window;
            if(this._MainZone != null)
            {
                this._MainZone._RenderRecursive(Context, win, size);
            }
            Context.Pop();
        }

        public override void Update(GUIControlContext Context, double Time)
        {
            if (this._Data != null)
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
                        this._Window = new Rectangle(nwinpos, nwinsize).Intersection(this.Data.Domain);
                    }
                }
            }

            // Action queue!
            lock (this)
            {
                List<Action> olqueue = this._ActionQueue;
                this._ActionQueue = new List<Action>();
                foreach (Action action in olqueue)
                {
                    action();
                }
            }

            // Update zones
            if (this._MainZone != null)
            {
                this._UpdateZone(this._MainZone, Time);
            }
        }

        private void _UpdateZone(PlotZone Zone, double Time)
        {
            const double FadeTime = 0.5;
            Zone._Fade = Math.Min(1.0, Zone._Fade + (Time / FadeTime));


            foreach (PlotZone subzone in Zone._SubZones)
            {
                this._UpdateZone(subzone, Time);
            }
        }

        /// <summary>
        /// Forces an area to be loaded.
        /// </summary>
        public void Load(Rectangle Area)
        {
            if (this._Data != null)
            {
                PlotZone pz = PlotZone._Create(this._Data.GetZone(Area), this._Multiplier, this._Gradient)();
                this._AddToTop(pz);
            }
        }

        /// <summary>
        /// Splits a zone into the specified amount (horizontally and vertically) of subzones. The main
        /// zone is kept.
        /// </summary>
        private void _Split(PlotZone Zone, int X, int Y)
        {
            Zone._Splitting = true;

            Rectangle area = Zone.Area;
            Point nsize = area.Size.Scale(new Point(1.0 / X, 1.0 / Y));
            int zones = X * Y;
            for (int x = 0; x < X; x++)
            {
                for (int y = 0; y < Y; y++)
                {
                    Rectangle rect = new Rectangle(area.Location + new Point(nsize.X * x, nsize.Y * y), nsize);
                    this._ThreadPool.AppendTask(delegate
                    {
                        Func<PlotZone> pz = PlotZone._Create(this._Data.GetZone(rect), this._Multiplier, this._Gradient);
                        lock (this)
                        {
                            this._ActionQueue.Add(delegate
                            {
                                Zone._SubZones.AddLast(pz());
                            });
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Adds the given zone above all other zones.
        /// </summary>
        private void _AddToTop(PlotZone Zone)
        {
            if (this._MainZone == null)
            {
                this._MainZone = Zone;
            }
            else
            {
                this._AddAbove(this._MainZone, Zone);
            }
        }

        /// <summary>
        /// Adds the target zone as the highest zone above the given zone.
        /// </summary>
        private void _AddAbove(PlotZone Cur, PlotZone Target)
        {
            LinkedListNode<PlotZone> onto = null;
            LinkedListNode<PlotZone> cur = Cur._SubZones.First;
            while (cur != null)
            {
                if (cur.Value.Area.Intersects(Target.Area))
                {
                    onto = cur;
                }
                cur = cur.Next;
            }
            if (onto == null)
            {
                Cur._SubZones.AddLast(Target);
            }
            else
            {
                this._AddAbove(onto.Value, Target);
            }
        }

        /// <summary>
        /// Gets or sets the area currently visible in the plot.
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
                if (this._Data != null)
                {
                    this._Window = this._Window.Intersection(this.Data.Domain);
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
        /// Gets or sets the data for the plot.
        /// </summary>
        public PlotData Data
        {
            get
            {
                return this._Data;
            }
            set
            {
                if (this._Data != value)
                {
                    if (this._MainZone != null)
                    {
                        this._MainZone._Delete();
                    }
                    this._Data = value;
                    this._Window = this._Data.Domain;
                    this.Load(this.Data.Domain);
                    this._Split(this._MainZone, 9, 9);
                }
            }
        }

        protected override void OnDispose()
        {
            if (this._MainZone != null)
            {
                this._MainZone._Delete();
            }
        }

        private PlotData _Data;
        private PlotZone _MainZone;
        private Rectangle _Window;
        private Gradient _Gradient;
        private double _Multiplier;
        private ThreadPool _ThreadPool;
        private List<Action> _ActionQueue;
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
            GL.DeleteTexture(this._Texture);
            foreach (PlotZone subzone in this._SubZones)
            {
                subzone._Delete();
            }
        }


        /// <summary>
        /// Renders the zone and subzones to the given context.
        /// </summary>
        internal void _RenderRecursive(GUIRenderContext Context, Rectangle Window, Point Size)
        {
            if (this._Area.Intersects(Window))
            {
                Rectangle rel = Window.ToRelative(this._Area);
                rel.Location.Y = 1.0 - rel.Size.Y - rel.Location.Y;
                rel = rel.Scale(Size);
                Context.DrawTexture(this._Texture, Color.RGBA(1.0, 1.0, 1.0, this._Fade), rel);

                foreach (PlotZone pz in this._SubZones)
                {
                    pz._RenderRecursive(Context, Window, Size);
                }
            }
        }

        /// <summary>
        /// Creates a plot zone from a data source. Note that the function returned has to be called on the main thread (in order to
        /// create textures).
        /// </summary>
        internal static Func<PlotZone> _Create(PlotData.Zone Source, double Multiplier, Gradient Gradient)
        {
            double pixels = 128 * 128;
            double sw = Math.Sqrt(pixels * Source.SampleRatio);
            double sh = pixels / sw;
            int w = (int)sw;
            int h = (int)sh;
            _TextureOutput to = new _TextureOutput(w, h, Multiplier, Gradient);
            Source.GetData<_TextureOutput>(w, h, to);
            return delegate
            {
                return new PlotZone()
                {
                    _Texture = to.CreateTexture(),
                    _Area = Source.Area,
                    _Fade = 0.0,
                    _Source = Source,
                };
            };
        }

        private class _TextureOutput : PlotData.Zone.IDataOutput
        {
            public _TextureOutput(int Width, int Height, double Multiplier, Gradient Gradient)
            {
                this._Width = Width;
                this._Height = Height;
                this._Data = new byte[Width * Height * 4];
                this._Multiplier = Multiplier;
                this._Gradient = Gradient;
            }

            public void Set(int X, int Y, double Value)
            {
                Value *= this._Multiplier;
                Color col = this._Gradient.GetColor(Value);
                byte r = (byte)(col.R * 255.0);
                byte g = (byte)(col.G * 255.0);
                byte b = (byte)(col.B * 255.0);
                int i = (X + Y * this._Width) * 4;
                byte[] data = this._Data;
                data[i + 0] = b;
                data[i + 1] = g;
                data[i + 2] = r;
                data[i + 3] = 255;
            }

            public int CreateTexture()
            {
                int id = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, id);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, this._Width, this._Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, this._Data);
                GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float)TextureEnvMode.Modulate);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureWrapMode.ClampToEdge);
                return id;
            }

            private int _Width;
            private int _Height;
            private byte[] _Data;
            private double _Multiplier;
            private Gradient _Gradient;
        }

        internal bool _Splitting;
        private int _Texture;
        internal double _Fade;
        private Rectangle _Area;
        internal LinkedList<PlotZone> _SubZones;
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