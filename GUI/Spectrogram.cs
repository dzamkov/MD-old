using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;

namespace MD.GUI
{
    public partial class Spectrogram : UserControl
    {
        public Spectrogram()
        {
            InitializeComponent();
            this._WindowSize = 1024;
        }

        /// <summary>
        /// Gets or sets the source that is visualized.
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
                this._Refresh();
            }
        }

        /// <summary>
        /// Refreshes the spectrogram.
        /// </summary>
        private unsafe void _Refresh()
        {
            if (this._Source != null)
            {
                int windows = this._Source.Size / this._WindowSize;
                int paddedsize = windows * this._WindowSize;
                AudioSource source;
                if (paddedsize == this._Source.Size)
                {
                    source = this._Source;
                }
                else
                {
                    windows++;
                    paddedsize += this._WindowSize;
                    source = new PaddedSource(this._Source, paddedsize);
                }
                Complex[] cdata = new Complex[this._WindowSize];
                Complex[] ndata = new Complex[this._WindowSize];

                this._Bitmap = new Bitmap(windows, this._WindowSize);

                BitmapData bd = this._Bitmap.LockBits(new Rectangle(0, 0, this._Bitmap.Width, this._Bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                for (int x = 0; x < windows; x++)
                {
                    this._WriteWindow(bd, cdata, ndata, x, x, source);
                }
                this._Bitmap.UnlockBits(bd);
            }
            else
            {
                this._Bitmap = null;
            }
            this.BackgroundImage = this._Bitmap;
            this.Refresh();
        }

        private unsafe void _WriteWindow(BitmapData BitData, Complex[] CData, Complex[] NData, int Window, int X, AudioSource Source)
        {
            double[] input = new double[this._WindowSize * Source.Channels];
            Source.ReadDouble(Window * this._WindowSize, this._WindowSize, input, 0);

            for (int t = 0; t < this._WindowSize; t++)
            {
                CData[t] = input[t * Source.Channels];
            }
            
            // Fourier transform
            FFT.OnArray(false, CData, NData, this._WindowSize);

            // Write
            byte* offset = (byte*)(BitData.Scan0.ToPointer()) + 3 * X;
            double d = 2.0 / (double)(this._WindowSize);
            for (int t = 0; t < this._WindowSize; t++)
            {
                double abs = NData[t].Abs * d;
                Color col = _Gradient.GetColor(abs);
                offset[2] = (byte)(col.R * 255.0);
                offset[1] = (byte)(col.G * 255.0);
                offset[0] = (byte)(col.B * 255.0);
                offset += BitData.Stride;
            }
        }

        private static readonly Gradient _Gradient = new Gradient(
            Color.RGB(0.0, 0.0, 1.0), Color.RGB(0.5, 0.0, 0.0),
            new Gradient.Stop[]
            {
                new Gradient.Stop(Color.RGB(0.0, 1.0, 1.0), 0.35),
                new Gradient.Stop(Color.RGB(0.0, 1.0, 0.0), 0.5),
                new Gradient.Stop(Color.RGB(1.0, 1.0, 0.0), 0.6),
                new Gradient.Stop(Color.RGB(1.0, 0.0, 0.0), 0.85)
            });

        private int _WindowSize;
        private AudioSource _Source;
        private Bitmap _Bitmap;
    }
}
