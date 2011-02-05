using System;
using System.Collections.Generic;

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
                    new Gradient.Stop(Color.RGB(0.6, 0.6, 0.6), 0.1),
                    new Gradient.Stop(Color.RGB(0.2, 0.2, 1.0), 0.3),
                    new Gradient.Stop(Color.RGB(0.0, 1.0, 1.0), 0.5),
                    new Gradient.Stop(Color.RGB(0.0, 1.0, 0.0), 0.7),
                    new Gradient.Stop(Color.RGB(1.0, 1.0, 0.0), 0.8),
                    new Gradient.Stop(Color.RGB(1.0, 0.0, 0.0), 0.9)
                });

            this._DataRects = new LinkedList<_DataRect>();
            this._Window = new Rectangle(0.0, 0.0, 0.5, 1000.0);
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

                _DataRect data = (_DataRect.Create(new Rectangle(0.0, 0.0, 0.5, 1000.0), 512, 256));
                data.Fill(value, this._Gradient, 0.01);
                this._DataRects.AddLast(data);
            }
        }

        public override void Render(GUIRenderContext Context)
        {
            Point size = this.Size;
            Rectangle win = this._Window;
            foreach (_DataRect dr in this._DataRects)
            {
                Rectangle area = dr.Area;
                if (area.Intersects(win))
                {
                    Rectangle rel = win.ToRelative(area);
                    rel.Location.Y += 1.0 - rel.Size.Y;
                    rel = rel.Scale(size);
                    Context.DrawTexture(dr.Texture, rel);
                }
            }
        }

        public override void Update(GUIControlContext Context, double Time)
        {
            
        }

        private static int _MakeTexture(int Width, int Height, byte[] Data)
        {
            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Width, Height, 0, PixelFormat.Bgr, PixelType.UnsignedByte, Data);
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float)TextureEnvMode.Modulate);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureWrapMode.Clamp);
            return id;
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
            /// Fills the data rectangle with data from an audio source using the gabor transform.
            /// </summary>
            public void Fill(AudioSource Source, Gradient Gradient, double GaussScale)
            {
                int w = this.TimeResolution;
                int h = this.FrequencyResolution;

                // Calculate sizes
                double gaussfalloff = 0.1913 * GaussScale;
                int sr = Source.SampleRate;
                int c = Source.Channels;
                int ts = (int)(sr * this.Area.Size.X);
                int pad = (int)(sr * gaussfalloff);
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
                byte[] dat = new byte[w * h * 3];
                for (int x = 0; x < w; x++)
                {
                    int midsample = pad + (int)((x / (double)w) * ts);
                    for (int y = 0; y < h; y++)
                    {
                        Complex val = new Complex(0.0, 0.0);
                        double freq = (Area.Location.Y + Area.Size.Y * ((h - y - 1) / (double)h)) / (double)sr;
                        double gausssize = 1.0 / (sr * GaussScale);
                        for (int z = -pad + 1; z < pad; z++)
                        {
                            int sample = z + midsample;
                            double sampleval = sdat[sample * c];
                            double gaussdis = Math.Abs(z) * gausssize;
                            double exponent = -Math.PI * 2.0 * freq * sample;
                            val += Math.Exp(-Math.PI * gaussdis * gaussdis) * new Complex(0.0, exponent).Exp * sampleval;
                        }

                        double aval = Math.Abs(val.Abs * 0.1);
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

                this.Texture = _MakeTexture(w, h, dat);
            }

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

        private LinkedList<_DataRect> _DataRects;
        private Rectangle _Window;
        private Gradient _Gradient;
        private AudioSource _Source;
    }
}