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
    /// An interactive spectrogram created for an audio source.
    /// </summary>
    public class Spectrogram : View
    {
        public Spectrogram(AudioSource Source)
        {
            this._Data = new SpectrogramData(Source);
            this._Plot = new Plot(this._Data);
            this.Window = this.Domain;
        }

        public override Rectangle Domain
        {
            get
            {
                return this._Data.Domain;
            }
        }

        public override void Render(GUIRenderContext Context)
        {
            this._Plot.Render(Context, this.Size, this.Window);
        }

        public override void Update(GUIControlContext Context, double Time)
        {
            this._Plot.Update(this.Window, Time);
            base.Update(Context, Time);
        }

        protected override void OnDispose()
        {
            this._Plot.Dispose();
            base.OnDispose();
        }

        private SpectrogramData _Data;
        private Plot _Plot;
    }

    /// <summary>
    /// Plot data for a spectrogram of an audio source.
    /// </summary>
    public class SpectrogramData : PlotData
    {
        public SpectrogramData(AudioSource Source)
        {
            this._Source = Source;
        }

        public override Rectangle Domain
        {
            get
            {
                AudioSource source = this._Source;
                return new Rectangle(0.0, 0.0, (double)source.Size / source.SampleRate, source.SampleRate / 2.0);
            }
        }

        public override PlotData.Zone GetZone(Rectangle Area)
        {
            return new Zone(Area, this._Source);
        }

        /// <summary>
        /// Zone for spectrogram data.
        /// </summary>
        public new class Zone : PlotData.Zone
        {
            public Zone(Rectangle Area, AudioSource Source)
            {
                this._Area = Area;
                this._Source = Source;

                double minfreq = Area.Location.Y;
                double maxfreq = minfreq + Area.Size.Y;
                double meanfreq = (maxfreq - minfreq) / (Math.Log(maxfreq) - Math.Log(minfreq + 1.0));
                double logmean = Math.Log(meanfreq);
                const double targmean = 5.7;
                this._MeanOffset = (logmean - targmean);
            }

            /// <summary>
            /// Gets the size of the window to use to perform the fourier transform.
            /// </summary>
            public int WindowSize
            {
                get
                {
                    return 2048 >> (int)(this._MeanOffset);
                }
            }

            /// <summary>
            /// Gets the scale of the gaussian window to use for the gabor transform.
            /// </summary>
            public double GaussScale
            {
                get
                {
                    return 0.028 * Math.Pow(0.5, this._MeanOffset);
                }
            }

            public override double SampleRatio
            {
                get
                {
                    return Math.Pow(1.1, this._MeanOffset) / Math.Pow(0.9, this._MeanOffset);
                }
            }

            public override Rectangle Area
            {
                get
                {
                    return this._Area;
                }
            }

            public override void GetData<TOutput>(int Width, int Height, TOutput Output)
            {
                double[] window = _CreateGaborWindow(this.GaussScale, this._Source.SampleRate, this.WindowSize);

                // Calculate sizes
                int sr = this._Source.SampleRate;
                int c = this._Source.Channels;
                int ts = (int)(sr * this.Area.Size.X);
                int pad = window.Length;
                int fs = ts + pad + pad;

                // Read required data
                double[] sdat = new double[fs * c];
                int sourcesize = this._Source.Size;
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
                    this._Source.ReadDouble(start, readsize, sdat, offset);
                }

                // Begin filling data
                Complex[] fftoutput = new Complex[window.Length * 2];
                for (int x = 0; x < Width; x++)
                {
                    int midsample = pad + (int)((x / (double)Width) * ts);
                    _WindowedSignal ws = new _WindowedSignal(window, sdat, midsample, c);
                    FFT.OnSignal<_WindowedSignal>(ws, false, fftoutput, fftoutput.Length);
                    for (int y = 0; y < Height; y++)
                    {
                        double freq = this.Area.Location.Y + this.Area.Size.Y * ((Height - y - 1) / (double)Height);
                        double fftsamp = FFT.GetSample(freq, fftoutput.Length, sr);
                        int isamp = (int)fftsamp;
                        double rsamp = fftsamp - (double)isamp;
                        Complex val = fftoutput[isamp];
                        Complex nval = fftoutput[isamp];

                        double aval = val.Abs * (1.0 - rsamp) + nval.Abs * rsamp;
                        Output.Set(x, y, aval);
                    }
                }
            }

            private double _MeanOffset;
            private Rectangle _Area;
            private AudioSource _Source;
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

        private AudioSource _Source;
    }
}