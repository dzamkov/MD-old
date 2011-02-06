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

            this._ActionQueue = new List<_Action>();
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
                this.BeingLoad(new Rectangle(0.0, 0.0, 10.0, 3000.0));
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
                    this.BeingLoad(this._Window);
                }
            }

            // Action queue time
            foreach (_Action a in this._ActionQueue)
            {
                a();
            }
            this._ActionQueue.Clear();

            // Foreach rect
            const double FadeRate = 0.4;
            LinkedListNode<_DataRect> cur = this._DataRects.First;
            while (cur != null)
            {
                LinkedListNode<_DataRect> next = cur.Next;
                _DataRect rect = cur.Value;
                rect.Fade = Math.Min(1.0, rect.Fade + (Time / FadeRate));


                cur = next;
            }
        }

        private static int _MakeTexture(int Width, int Height, byte[] Data)
        {
            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Width, Height, 0, PixelFormat.Bgr, PixelType.UnsignedByte, Data);
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
            double iscale = 1.0 / Scale;
            for (int t = 0; t < Size; t++)
            {
                double exp = ((double)t / SampleRate) * iscale;
                win[t] = Math.Exp(-exp * exp);
            }
            return win;
        }

        /// <summary>
        /// Begins loading a time-frequency rectangle. The data will be shown when it is done loading.
        /// </summary>
        public void BeingLoad(Rectangle Rectangle)
        {
            AudioSource source = this._Source;
            Gradient grad = this._Gradient;
            Thread th = new Thread(delegate()
                {
                    _DataRect data = _DataRect.Create(Rectangle, 128, 128);
                    double[] win = _CreateGaborWindow(0.024, source.SampleRate, 2048);
                    _Action maketexture = data.Fill(source, grad, win);
                    this._ActionQueue.Add(delegate
                    {
                        maketexture();
                        this._DataRects.AddLast(data);
                    });
                });
            th.IsBackground = true;
            th.Start();
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
            public _Action Fill(AudioSource Source, Gradient Gradient, double[] Window)
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
                Source.ReadDouble(start, readsize, sdat, offset);

                // Begin filling data
                Complex[] fftoutput = new Complex[Window.Length * 2];
                double d = 0.5 / fftoutput.Length;

                byte[] dat = new byte[w * h * 3];
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

                        double aval = (val.Abs * (1.0 - rsamp) + nval.Abs * rsamp) * d * 1000.0;
                        aval = Math.Min(1.0, aval);
                        Color col = Gradient.GetColor(aval);
                        byte r = (byte)(col.R * 255);
                        byte g = (byte)(col.G * 255);
                        byte b = (byte)(col.B * 255);

                        int i = (x + y * w) * 3;
                        dat[i + 0] = b;
                        dat[i + 1] = g;
                        dat[i + 2] = r;
                    }
                }

                return delegate
                {
                    this.Texture = _MakeTexture(w, h, dat);
                };
            }

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

        private delegate void _Action();

        private List<_Action> _ActionQueue;
        private LinkedList<_DataRect> _DataRects;
        private Rectangle _Window;
        private Gradient _Gradient;
        private AudioSource _Source;
    }
}