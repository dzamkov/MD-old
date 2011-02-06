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
    /// A spectrogram, without any other controls or measurements.
    /// </summary>
    public class Spectrogram : Control
    {
        public Spectrogram()
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

            this._ActionQueue = new List<Action>();
            this._DataRects = new LinkedList<_DataRect>();
            this._Window = new Rectangle(0.0, 0.0, 10.0, 3000.0);
        }

        /// <summary>
        /// Gets or sets the source audio data for the spectrogram.
        /// </summary>
        public AudioSource Source
        {
            get
            {
                return this._Source;
            }
            set
            {
                this._Source = value;
                this.BeginLoad(new Rectangle(0.0, 0.0, 10.0, 3000.0));
            }
        }

        public override void Render(GUIRenderContext Context)
        {
            Point size = this.Size;
            Context.PushClip(new Rectangle(size));

            Rectangle win = this._Window;
            foreach (_DataRect dr in this._DataRects)
            {
                Rectangle area = dr.Area;
                if (area.Intersects(win))
                {
                    Rectangle rel = win.ToRelative(area);
                    rel.Location.Y = 1.0 - rel.Size.Y - rel.Location.Y;
                    rel = rel.Scale(size);
                    Context.DrawTexture(dr.Texture, Color.RGBA(1.0, 1.0, 1.0, dr.Fade), rel);
                }
            }

            Context.Pop();
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
                    this._Window = new Rectangle(nwinpos, nwinsize);
                }
                if (ms.HasReleasedButton(OpenTK.Input.MouseButton.Left))
                {
                    this.BeginLoad(this._Window);
                }
            }
            if (this._Source != null)
            {
                this._Window = this._Window.Intersection(this.SourceRectangle);
            }

            // Action queue time
            List<Action> prevactions = this._ActionQueue;
            this._ActionQueue = new List<Action>();
            foreach (Action a in prevactions)
            {
                a();
            }

            // Foreach rect
            const double FadeRate = 0.4;
            LinkedListNode<_DataRect> cur = this._DataRects.First;
            while (cur != null)
            {
                LinkedListNode<_DataRect> next = cur.Next;
                _DataRect rect = cur.Value;
                if (rect.Removing)
                {
                    rect.Fade -= (Time / FadeRate);
                    if (rect.Fade <= 0.0)
                    {
                        this._DataRects.Remove(cur);
                    }
                }
                else
                {
                    rect.Fade = Math.Min(1.0, rect.Fade + (Time / FadeRate));
                    if (!rect.Splitting && rect.Area.Size.X > 6.0)
                    {
                        rect.Split(this);
                    }
                }
                cur = next;
            }
        }

        private static int _MakeTexture(int Width, int Height, byte[] Data)
        {
            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, Data);
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float)TextureEnvMode.Modulate);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureWrapMode.ClampToEdge);
            return id;
        }

        /// <summary>
        /// Creates a window for the gabor transform.
        /// </summary>
        private static double[] _CreateGaborWindow(double Scale, int SampleRate, int Size)
        {
            double[] win = new double[Size];
            double total = 0.0;
            double iscale = 1.0 / Scale;
            for (int t = 0; t < Size; t++)
            {
                double exp = ((double)t / SampleRate) * iscale;
                total += win[t] = Math.Exp(-exp * exp);
            }
            double itotal = 1.0 / total;
            for (int t = 0; t < Size; t++)
            {
                win[t] *= itotal;
            }
            return win;
        }

        /// <summary>
        /// Begins loading a time-frequency rectangle. The data will be shown when it is done loading.
        /// </summary>
        public void BeginLoad(Rectangle Rectangle)
        {
            this.BeginLoad(Rectangle, null);
        }

        /// <summary>
        /// Begins loading a time-frequency rectangle. The data will be shown when it is done loading.
        /// </summary>
        public void BeginLoad(Rectangle Rectangle, Action OnLoad)
        {
            AudioSource source = this._Source;
            if (source != null)
            {
                Rectangle sourcerect = this.SourceRectangle;
                if (sourcerect.Intersects(Rectangle))
                {
                    Rectangle = sourcerect.Intersection(Rectangle);
                    Gradient grad = this._Gradient;
                    Thread th = new Thread(delegate()
                        {
                            int tsamps;
                            int fsamps;
                            double[] win;
                            this._EstimateRectParameters(Rectangle, source.SampleRate, out tsamps, out fsamps, out win);
                            _DataRect data = _DataRect.Create(Rectangle, tsamps, fsamps);
                            Action maketexture = data.Fill(source, grad, win);
                            this._ActionQueue.Add(delegate
                            {
                                maketexture();
                                this._DataRects.AddLast(data);
                                if (OnLoad != null)
                                {
                                    OnLoad();
                                }
                            });
                        });
                    th.IsBackground = true;
                    th.Start();
                }
                else
                {
                    // Nothing to load.
                    OnLoad();
                }
            }
        }

        /// <summary>
        /// Gets the time-frequency domain rectangle for the source.
        /// </summary>
        public Rectangle SourceRectangle
        {
            get
            {
                AudioSource source = this.Source;
                return new Rectangle(0.0, 0.0, (double)source.Size / source.SampleRate, source.SampleRate / 2.0);
            }
        }

        /// <summary>
        /// Estimates paramters for creating a data rectangle.
        /// </summary>
        private void _EstimateRectParameters(Rectangle Rectangle, int SampleRate, out int TSamples, out int FSamples, out double[] Window)
        {
            TSamples = 128;
            FSamples = 128;

            double minfreq = Rectangle.Location.Y;
            double maxfreq = minfreq + Rectangle.Size.Y;
            double meanfreq = (maxfreq - minfreq) / (Math.Log(maxfreq) - Math.Log(minfreq + 1));
            double logmean = Math.Log(meanfreq);
            double gaussscale = 0.144 / logmean;
            const double targmean = 5.7;
            int pow = (int)(logmean - targmean);
            int winsize = 2048;
            while (pow > 0)
            {
                winsize /= 2;
                pow--;
            }
            while (pow < 0)
            {
                winsize *= 2;
                pow++;
            }
            TSamples = (int)(TSamples * Math.Pow(2.0, logmean - targmean));
            FSamples = (int)(FSamples * Math.Pow(0.7, logmean - targmean));
            Window = _CreateGaborWindow(gaussscale, SampleRate, winsize);
        }

        /// <summary>
        /// A rectangle containing data (with a texture, and in memory) for the spectrogram.
        /// </summary>
        private class _DataRect
        {
            /// <summary>
            /// Creates a data rectangle.
            /// </summary>
            public static _DataRect Create(Rectangle Area, int TimeResolution, int FrequencyResolution)
            {
                _DataRect dr = new _DataRect();
                dr.Area = Area;
                dr.TimeResolution = TimeResolution;
                dr.FrequencyResolution = FrequencyResolution;
                return dr;
            }

            /// <summary>
            /// Fills the data rectangle with data from an audio source using a transform with a symmetric, power-of-2-sized window. Returns an
            /// action that needs to be called on the main thread in order to build the texture.
            /// </summary>
            public Action Fill(AudioSource Source, Gradient Gradient, double[] Window)
            {
                int w = this.TimeResolution;
                int h = this.FrequencyResolution;

                // Calculate sizes
                int sr = Source.SampleRate;
                int c = Source.Channels;
                int ts = (int)(sr * this.Area.Size.X);
                int pad = Window.Length;
                int fs = ts + pad + pad;

                // Read required data
                double[] sdat = new double[fs * c];
                int sourcesize = Source.Size;
                int readsize = fs;
                int start = (int)(sr * this.Area.Location.X) - pad;
                int offset = 0;
                if(start < 0)
                {
                    readsize += start;
                    offset -= start * c;
                    start = 0;
                }
                if(readsize + start >= sourcesize)
                {
                    readsize = sourcesize - start;
                }
                if (readsize > 0)
                {
                    Source.ReadDouble(start, readsize, sdat, offset);
                }

                // Begin filling data
                Complex[] fftoutput = new Complex[Window.Length * 2];

                byte[] dat = new byte[w * h * 4];
                for (int x = 0; x < w; x++)
                {
                    int midsample = pad + (int)((x / (double)w) * ts);
                    _WindowedSignal ws = new _WindowedSignal(Window, sdat, midsample, c);
                    FFT.OnSignal<_WindowedSignal>(ws, false, fftoutput, fftoutput.Length);

                    for (int y = 0; y < h; y++)
                    {
                        double freq = this.Area.Location.Y + this.Area.Size.Y * ((h - y - 1) / (double)h);
                        double fftsamp = FFT.GetSample(freq, fftoutput.Length, sr);
                        int isamp = (int)fftsamp;
                        double rsamp = fftsamp - (double)isamp;
                        Complex val = fftoutput[isamp];
                        Complex nval = fftoutput[isamp];

                        double aval = (val.Abs * (1.0 - rsamp) + nval.Abs * rsamp) * 400.0;
                        aval = Math.Min(1.0, aval);
                        Color col = Gradient.GetColor(aval);
                        byte r = (byte)(col.R * 255);
                        byte g = (byte)(col.G * 255);
                        byte b = (byte)(col.B * 255);

                        int i = (x + y * w) * 4;
                        dat[i + 0] = b;
                        dat[i + 1] = g;
                        dat[i + 2] = r;
                        dat[i + 3] = 255;
                    }
                }

                return delegate
                {
                    this.Texture = _MakeTexture(w, h, dat);
                };
            }

            /// <summary>
            /// Causes the data rect to subdivide into 4 parts.
            /// </summary>
            public void Split(Spectrogram Spectrogram)
            {
                this.Splitting = true;
                Rectangle area = this.Area;
                Point subsize = area.Size * 0.5;
                Rectangle[] subrects = new Rectangle[]
                {
                    new Rectangle(area.Location + new Point(0.0, 0.0), subsize),
                    new Rectangle(area.Location + new Point(subsize.X, 0.0), subsize),
                    new Rectangle(area.Location + new Point(0.0, subsize.Y), subsize),
                    new Rectangle(area.Location + new Point(subsize.X, subsize.Y), subsize),
                };
                bool[] complete = new bool[4];
                for (int t = 0; t < 4; t++)
                {
                    int index = t;
                    Spectrogram.BeginLoad(subrects[index], delegate
                    {
                        complete[index] = true;

                        // If all split parts are done loading, remove the original rect.
                        for (int i = 0; i < 4; i++)
                        {
                            if (!complete[i])
                            {
                                return;
                            }
                        }
                        this.Removing = true;
                    });
                }
            }

            /// <summary>
            /// Gets if the data is in the process of being removed.
            /// </summary>
            public bool Removing;

            /// <summary>
            /// Gets if the data rect is currently being split.
            /// </summary>
            public bool Splitting;

            /// <summary>
            /// How visible the data rect is.
            /// </summary>
            public double Fade;

            /// <summary>
            /// Gets the amount of samples in the time-domain of the rectangle.
            /// </summary>
            public int TimeResolution;

            /// <summary>
            /// Gets the amount of samples in the frequency-domain of the rectangle.
            /// </summary>
            public int FrequencyResolution;

            /// <summary>
            /// Gets the texture for this data.
            /// </summary>
            public int Texture;

            /// <summary>
            /// Gets the area this data occupies in the time-frequency domain. The X component of the
            /// rectangle gives the seconds into the audio source this data is and the Y component
            /// gives the frequency in hertz of the data. Note that the location of the rectangle
            /// is the lowest point in both components, and the location of the first sample.
            /// </summary>
            public Rectangle Area;
        }

        /// <summary>
        /// A signal created by applying a symmetric window to an array.
        /// </summary>
        private class _WindowedSignal : IComplexSignal
        {
            public _WindowedSignal(double[] Window, double[] Source, int Offset, int Channels)
            {
                this._Window = Window;
                this._Source = Source;
                this._Offset = Offset;
                this._Channels = Channels;
            }

            public Complex Get(int Index)
            {
                Index -= this._Window.Length;
                int windex = Index;
                int sindex = this._Offset + Index;
                if (windex < 0) windex = -1 - windex;
                return this._Source[sindex * this._Channels] * this._Window[windex];
            }

            private double[] _Window;
            private double[] _Source;
            private int _Offset;
            private int _Channels;
        }

        private List<Action> _ActionQueue;
        private LinkedList<_DataRect> _DataRects;
        private Rectangle _Window;
        private Gradient _Gradient;
        private AudioSource _Source;
    }
}