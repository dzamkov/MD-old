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
    public class SpectrogramView : View
    {
        public SpectrogramView(AudioSource Source)
        {
            this._Graph = new SpectrogramGraph(Source);
            this.Window = this.Domain;
        }

        public override Rectangle Domain
        {
            get
            {
                return this._Graph.Domain;
            }
        }

        public override void Render(GUIRenderContext Context)
        {
            base.Render(Context);
            this._Graph.Render(Context, this.Size, this.Window);
        }

        public override void Update(GUIControlContext Context, double Time)
        {
            base.Update(Context, Time);
            this._Graph.Update(this.Window, Time);
        }

        protected override void OnDispose()
        {

        }

        private SpectrogramGraph _Graph;
    }

    /// <summary>
    /// A graph that shows a spectrogram.
    /// </summary>
    public class SpectrogramGraph : Graph
    {
        public SpectrogramGraph(AudioSource Source)
        {
            this._Source = Source;
            this._TexturesPerNode = 4;
            this._SpectrumWindow = Spectrogram.CreateGaborWindow(0.028, this._Source.SampleRate, 4096);
            this._Root = new _RenderNode(this._TexturesPerNode);
            this._Root.Fill(0, SpectrogramNode.GetRootNodeSize(this._Source.Size), 64);

            this._LastWindow = this.Domain;
            this._LoadPool = new ThreadPool(this._NextTask);
            this._LoadPool.ThreadAmount = 3;
            this._LoadPool.Start();
        }

        public override Rectangle Domain
        {
            get
            {
                return Spectrogram.GetDomain(this._Source.SampleRate, this._Source.Size);
            }
        }

        public override void Render(GUIRenderContext Context, Point Size, Rectangle Window)
        {
            this._Root.Render(Context, Size, Window, this._Source, this._SpectrumWindow);
        }

        public override void Update(Rectangle Window, double Time)
        {
            this._LastWindow = Window;
        }

        /// <summary>
        /// A node with rendering information.
        /// </summary>
        private class _RenderNode : SpectrogramNode<_RenderNode>
        {
            public _RenderNode(int TexturesPerNode)
            {
                this.Textures = new int[TexturesPerNode];
            }

            /// <summary>
            /// Creates one of the textures for this node.
            /// </summary>
            public int CreateTexture(int Texture, AudioSource Source, double[] SpectrumWindow)
            {
                int w = this.TimeSamples;
                int fh = SpectrumWindow.Length;
                int h = fh / this.Textures.Length;
                int o = Texture * h;
                Complex[][] data = this.Data;
                double[] texdata = new double[h * w];
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        texdata[x + y * w] = data[x][(fh - y - o - 1)].Abs;
                    }
                }
                return this.Textures[Texture] = Plot.CreateTexture(w, h, texdata, 400.0, Plot.DefaultGradient);
            }

            /// <summary>
            /// Renders this node.
            /// </summary>
            public void Render(GUIRenderContext Context, Point Size, Rectangle Window, AudioSource Source, double[] SpectrumWindow)
            {
                if (this.Loaded)
                {
                    bool needrender = (this.LeftSubNode == null || !this.LeftSubNode.Loaded) || (this.RightSubNode == null || !this.RightSubNode.Loaded);
                    if (needrender)
                    {
                        Rectangle area = this.GetArea(Source.SampleRate);
                        Point texturesize = area.Size; texturesize.Y /= this.Textures.Length;
                        for (int t = 0; t < this.Textures.Length; t++)
                        {
                            Rectangle texturerect = new Rectangle(new Point(area.Location.X, area.Location.Y + area.Size.Y - texturesize.Y * (t + 1)), texturesize);
                            if (Window.Intersects(texturerect))
                            {
                                int tex = this.Textures[t];
                                if (tex == 0)
                                {
                                    tex = this.CreateTexture(t, Source, SpectrumWindow);
                                }
                                Rectangle rel = Window.ToRelative(texturerect);
                                rel.Location.Y = 1.0 - rel.Size.Y - rel.Location.Y;
                                rel = rel.Scale(Size);
                                Context.DrawTexture(tex, rel);
                            }
                            else
                            {
                                int tex = this.Textures[t];
                            }
                        }
                    }
                    if (this.LeftSubNode != null)
                    {
                        this.LeftSubNode.Render(Context, Size, Window, Source, SpectrumWindow);
                    }
                    if (this.RightSubNode != null)
                    {
                        this.RightSubNode.Render(Context, Size, Window, Source, SpectrumWindow);
                    }
                }
            }

            /// <summary>
            /// Gets the next node that needs loading.
            /// </summary>
            public _RenderNode NextToLoad(Rectangle LastWindow, AudioSource Source, out double Priority)
            {
                Priority = 0.0;
                _RenderNode res = null;
                if (!this.Loaded)
                {
                    double vis = _Visibility(this.GetArea(Source.SampleRate), LastWindow);
                    res = this;
                    Priority = vis;
                }
                if (this.LeftSubNode != null)
                {
                    double tp;
                    _RenderNode t = this.LeftSubNode.NextToLoad(LastWindow, Source, out tp);
                    if (tp > Priority)
                    {
                        Priority = tp;
                        res = t;
                    }
                }
                if (this.RightSubNode != null)
                {
                    double tp;
                    _RenderNode t = this.RightSubNode.NextToLoad(LastWindow, Source, out tp);
                    if (tp > Priority)
                    {
                        Priority = tp;
                        res = t;
                    }
                }
                return res;
            }

            public int[] Textures;
        }

        /// <summary>
        /// Gets the portion of common area (out of the total area of B) the rectangles have.
        /// </summary>
        private static double _Visibility(Rectangle A, Rectangle B)
        {
            if (A.Intersects(B))
            {
                Rectangle i = A.Intersection(B);
                return i.Size.X / B.Size.X * i.Size.Y / B.Size.Y;
            }
            return 0.0;
        }

        /// <summary>
        /// Gets the next task for the load thread pool.
        /// </summary>
        private Action _NextTask()
        {
            double priority;
            _RenderNode rn = this._Root.NextToLoad(this._LastWindow, this._Source, out priority);
            if (rn != null && priority > 0.1)
            {
                return delegate
                {
                    rn.LoadFast(this._Source, this._SpectrumWindow);
                    rn.Split(new _RenderNode(this._TexturesPerNode), new _RenderNode(this._TexturesPerNode));
                };
            }
            else
            {
                return null;
            }
        }

        private int _TexturesPerNode;
        private double[] _SpectrumWindow;
        private ThreadPool _LoadPool;
        private Rectangle _LastWindow;
        private _RenderNode _Root;
        private AudioSource _Source;
    }

    /// <summary>
    /// Contains spectrogram-related functionality.
    /// </summary>
    public class Spectrogram
    {
        /// <summary>
        /// Gets the spectrogram domain for an audio source of the specified sample rate and size.
        /// </summary>
        public static Rectangle GetDomain(int SampleRate, int Size)
        {
            return new Rectangle(0.0, 0.0, Size / (double)SampleRate, (double)SampleRate / 2.0);
        }

        /// <summary>
        /// Creates a window for the gabor transform, used to create a spectrogram.
        /// </summary>
        public static double[] CreateGaborWindow(double Scale, int SampleRate, int Size)
        {
            double[] win = new double[Size];
            double total = 0.0;
            double iscale = 1.0 / Scale;
            for (int t = 0; t < Size; t++)
            {
                double exp = ((double)t / SampleRate) * iscale;
                total += win[t] = Math.Exp(-exp * exp);
            }

            // Make sure that all the values in the full window add up to one.
            double itotal = 0.5 / total;
            for (int t = 0; t < Size; t++)
            {
                win[t] *= itotal;
            }
            return win;
        }

        /// <summary>
        /// Calculates a frequency spectrum for a sample in an audio source using the specified symmetric window. The size of the output
        /// will be the same as the size of the window.
        /// </summary>
        public static Complex[] CalculateSample(AudioSource Source, double[] Window, int Sample)
        {
            int c = Source.Channels;
            int hwinsize = Window.Length;
            int winsize = hwinsize * 2;
            Complex[] output = new Complex[winsize];
            double[] data = new double[winsize * c];
            Source.ReadDoublePad(Sample - hwinsize, winsize, data, 0);

            _WindowedSignal signal = new _WindowedSignal(data, Window, c);
            FFT.OnSignal<_WindowedSignal>(signal, false, output, winsize);

            // The upper half of the output is redundant.
            Complex[] houtput = new Complex[hwinsize];
            for (int t = 0; t < hwinsize; t++)
            {
                houtput[t] = output[t];
            }

            return houtput;
        }

        /// <summary>
        /// Creates a precomputed table for the FFT operations needed to quickly produce samples.
        /// </summary>
        public static Complex[] PrecomputeFFTTable(int FullWindowSize)
        {
            Complex[] table = new Complex[FullWindowSize];
            double c = -2.0 * Math.PI;
            for (int i = 0; i < FullWindowSize; i++)
            {
                table[i] = new Complex(c * (double)i / (double)FullWindowSize).TimesI.Exp;
            }
            return table;
        }

        /// <summary>
        /// Calculates a spectrogram sample quickly. Requires the full window to be computed.
        /// </summary>
        public static unsafe Complex[] CalculateSampleFast(AudioSource Source, double* FullWindow, Complex* PrecomputedFFT, int FullWindowSize, int Sample)
        {
            int c = Source.Channels;
            int hwinsize = FullWindowSize / 2;
            int winsize = FullWindowSize;
            Complex[] output = new Complex[FullWindowSize];
            double[] data = new double[FullWindowSize * c];
            Source.ReadDoublePad(Sample - FullWindowSize / 2, FullWindowSize, data, 0);

            Complex[] houtput = new Complex[winsize];

            fixed (double* inputptr = data)
            {
                fixed (Complex* outputptr = output)
                {
                    _WindowFFT(inputptr, FullWindow, outputptr, PrecomputedFFT, FullWindowSize, 1, c);

                    // The upper half of the output is redundant.
                    for (int t = 0; t < hwinsize; t++)
                    {
                        houtput[t] = output[t];
                    }
                }
            }

            return houtput;
        }

        /// <summary>
        /// Input signal for a FFT on a windowed portion of the source data.
        /// </summary>
        private class _WindowedSignal : IComplexSignal
        {
            public _WindowedSignal(double[] Data, double[] Window, int Channels)
            {
                this._Data = Data;
                this._Window = Window;
                this._HWindowSize = Window.Length;
                this._Channels = Channels;
            }

            public Complex Get(int Index)
            {
                int winindex = Index - this._HWindowSize;
                if (winindex < 0)
                {
                    winindex = -1 - winindex;
                }
                return this._Data[Index * this._Channels] * this._Window[winindex];
            }

            private double[] _Data;
            private double[] _Window;
            private int _HWindowSize;
            private int _Channels;
        }

        private static unsafe void _WindowFFT(double* Input, double* FullWindow, Complex* Output, Complex* PrecomputedFFT, int Samples, int Step, int Channels)
        {
            if (Samples == 1)
            {
                *Output = (*Input) * (*FullWindow);
            }
            else
            {
                int hsamps = Samples / 2;
                int dstep = Step * 2;
                _WindowFFT(Input, FullWindow, Output, PrecomputedFFT, hsamps, dstep, Channels);
                _WindowFFT(Input + (Step * Channels), FullWindow + Step, Output + hsamps, PrecomputedFFT, hsamps, dstep, Channels);
                
                for (int i = 0; i < hsamps; i++)
                {
                    Complex t = Output[i];
                    Complex e = PrecomputedFFT[i * Step];
                    Complex es = e * Output[hsamps + i];
                    Output[i] = t + es;
                    Output[hsamps + i] = t - es;
                }
            }
        }
    }

    /// <summary>
    /// A section of evenly spaced samples in a spectrogram tree. Spectrogram nodes may contain
    /// children which have more densely packed samples over a smaller area.
    /// </summary>
    public class SpectrogramNode<TNode>
        where TNode : SpectrogramNode<TNode>
    {
        public SpectrogramNode()
        {
            this._Loading = true;
        }

        /// <summary>
        /// Gets the area of this node in the time-frequency domain given the sample rate of the input source.
        /// </summary>
        public Rectangle GetArea(int SampleRate)
        {
            return new Rectangle(this._Start / (double)SampleRate, 0.0, this._Size / (double)SampleRate, (double)SampleRate / 2.0);
        }

        /// <summary>
        /// Fills the data for the node.
        /// </summary>
        /// <param name="Start">The start sample of the node.</param>
        /// <param name="Size">The size of the node, must be a power of 2 equal or larger than TimeSamples</param>
        /// <param name="TimeSamples">The amount of time samples in a node.</param>
        public void Fill(int Start, int Size, int TimeSamples)
        {
            this._Data = new Complex[TimeSamples][];
            this._Start = Start;
            this._Size = Size;
            this._Loaded = false;
            this._Loading = false;
        }

        /// <summary>
        /// Gets the amount of samples in time this node has.
        /// </summary>
        public int TimeSamples
        {
            get
            {
                return this._Data.Length;
            }
        }

        /// <summary>
        /// Gets the left sub node (the first one in time) for this node.
        /// </summary>
        public TNode LeftSubNode
        {
            get
            {
                return this._LeftSubNode;
            }
        }

        /// <summary>
        /// Gets the right sub node for this node.
        /// </summary>
        public TNode RightSubNode
        {
            get
            {
                return this._RightSubNode;
            }
        }

        /// <summary>
        /// Loads the data for this node if it is not already loaded. This call is thread safe.
        /// </summary>
        public unsafe void LoadFast(AudioSource Source, double[] Window)
        {
            lock (this)
            {
                if (this.Loading || this.Loaded)
                {
                    return;
                }
                this._Loading = true;
            }
            int nodesamples = this._Data.Length;
            int step = this._Size / nodesamples;
            int cur = this._Start;

            int hwinsize = Window.Length;
            int winsize = hwinsize * 2;
            double[] fullwindow = new double[hwinsize * 2];
            for (int t = 0; t < hwinsize; t++)
            {
                fullwindow[t] = Window[hwinsize - t - 1];
                fullwindow[t + hwinsize] = Window[t];
            }

            Complex[] ffttable = Spectrogram.PrecomputeFFTTable(winsize);
            fixed (Complex* ffttableptr = ffttable)
            {
                fixed (double* fullwindowptr = fullwindow)
                {
                    for (int t = 0; t < nodesamples; t++)
                    {
                        if (this._Data[t] == null)
                        {
                            this._Data[t] = Spectrogram.CalculateSampleFast(Source, fullwindowptr, ffttableptr, hwinsize * 2, cur);
                        }
                        cur += step;
                    }
                }
            }
            this._Loaded = true;
            this._Loading = false;
        }

        /// <summary>
        /// Loads the data for this node if it is not already loaded. This call is thread safe.
        /// </summary>
        public void Load(AudioSource Source, double[] Window)
        {
            lock (this)
            {
                if (this.Loading || this.Loaded)
                {
                    return;
                }
                this._Loading = true;
            }
            int nodesamples = this._Data.Length;
            int step = this._Size / nodesamples;
            int cur = this._Start;

            int hwinsize = Window.Length;
            for (int t = 0; t < nodesamples; t++)
            {
                if (this._Data[t] == null)
                {
                    this._Data[t] = Spectrogram.CalculateSample(Source, Window, cur);
                }
                cur += step;
            }
            this._Loaded = true;
            this._Loading = false;
        }

        /// <summary>
        /// Sets and fills the left and right subnodes for this nodes. This node must be loaded for a sucsessful split.
        /// </summary>
        /// <returns>True on sucsess, false on failure (because the split is invalid)</returns>
        public bool Split(TNode Left, TNode Right)
        {
            int scount = this._Data.Length;
            if (this._Loaded && this._Size > scount)
            {
                // Set
                this._LeftSubNode = Left;
                this._RightSubNode = Right;

                // Fill
                int hsize = this._Size / 2;
                Left.Fill(this._Start, hsize, scount);
                Right.Fill(this._Start + hsize, hsize, scount);

                // Copy common samples
                int hscount = scount / 2;
                for (int t = 0; t < hscount; t++)
                {
                    Left._Data[t * 2] = this._Data[t];
                    Right._Data[t * 2] = this._Data[t + hscount];
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the data array for the node.
        /// </summary>
        public Complex[][] Data
        {
            get
            {
                return this._Data;
            }
        }

        /// <summary>
        /// Gets the first time sample in this node's region.
        /// </summary>
        public int Start
        {
            get
            {
                return this._Start;
            }
        }

        /// <summary>
        /// Gets the size of the node's region in samples.
        /// </summary>
        public int Size
        {
            get
            {
                return this._Size;
            }
        }

        /// <summary>
        /// Gets if all the sample's for the node have been loaded (not including descendants).
        /// </summary>
        public bool Loaded
        {
            get
            {
                return this._Loaded;
            }
        }

        /// <summary>
        /// Gets if this node is loading samples on another thread.
        /// </summary>
        public bool Loading
        {
            get
            {
                return this._Loading;
            }
        }

        private bool _Loading;
        private bool _Loaded;
        private int _Start;
        private int _Size;
        private Complex[][] _Data;
        private TNode _LeftSubNode;
        private TNode _RightSubNode;
    }

    /// <summary>
    /// A simple concrete spectrogram node. Also contains functions related to spectrogram nodes and trees.
    /// </summary>
    public class SpectrogramNode : SpectrogramNode<SpectrogramNode>
    {
        /// <summary>
        /// Gets the size of the root node needed to contain all the samples for a source of the given size.
        /// </summary>
        public static int GetRootNodeSize(int SourceSize)
        {
            SourceSize--;
            SourceSize |= SourceSize >> 1;
            SourceSize |= SourceSize >> 2;
            SourceSize |= SourceSize >> 4;
            SourceSize |= SourceSize >> 8;
            SourceSize |= SourceSize >> 16;
            SourceSize++;
            return SourceSize;
        }
    }
}