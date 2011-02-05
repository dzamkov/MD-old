using System;
using System.Collections.Generic;

namespace MD
{
    /// <summary>
    /// Contains functions for performing fourier transforms.
    /// </summary>
    public static class FFT
    {
        private static void _FFT<TSignal>(TSignal Input, bool Inverse, Complex[] Output, int InputOffset, int OutputOffset, int Samples, int Step)
            where TSignal : IComplexSignal
        {
            if (Samples == 1)
            {
                Output[OutputOffset] = Input.Get(InputOffset);
            }
            else
            {
                int hsamps = (Samples / 2);
                int dstep = Step * 2;
                _FFT<TSignal>(Input, Inverse, Output, InputOffset, OutputOffset, hsamps, dstep);
                _FFT<TSignal>(Input, Inverse, Output, InputOffset + Step, OutputOffset + hsamps, hsamps, dstep);
                double c = (Inverse ? 2.0 : -2.0) * Math.PI;
                for (int i = 0; i < hsamps; i++)
                {
                    Complex t = Output[OutputOffset + i];
                    Complex e = new Complex(c * (double)i / (double)Samples).TimesI.Exp;
                    Complex es = e * Output[OutputOffset + hsamps + i];
                    Output[OutputOffset + i] = t + es;
                    Output[OutputOffset + hsamps + i] = t - es;
                }
            }
        }
        /// <summary>
        /// Performs a fourier transform on the input data. Samples must be a power of two.
        /// </summary>
        public static void OnArray(bool Inverse, Complex[] Input, Complex[] Output, int InputOffset, int OutputOffset, int Samples)
        {
            OnSignal<ArrayComplexSignal>(new ArrayComplexSignal(Input), Inverse, Output, InputOffset, OutputOffset, Samples);
        }

        /// <summary>
        /// Performs a fourier transform on the input data. Samples must be a power of two.
        /// </summary>
        public static void OnArray(bool Inverse, Complex[] Input, Complex[] Output, int Samples)
        {
            OnArray(Inverse, Input, Output, 0, 0, Samples);
        }

        /// <summary>
        /// Performs a fourier transform on the input signal. Samples must be a power of two.
        /// </summary>
        public static void OnSignal<TSignal>(TSignal Signal, bool Inverse, Complex[] Output, int InputOffset, int OutputOffset, int Samples)
            where TSignal : IComplexSignal
        {
            _FFT<TSignal>(Signal, Inverse, Output, InputOffset, OutputOffset, Samples, 1);
            if (Inverse)
            {
                double d = 1.0 / (double)Samples;
                for (int t = 0; t < Samples; t++)
                {
                    Output[OutputOffset + t] *= d;
                }
            }
        }

        /// <summary>
        /// Performs a fourier transform on the input signal. Samples must be a power of two.
        /// </summary>
        public static void OnSignal<TSignal>(TSignal Signal, bool Inverse, Complex[] Output, int Samples)
            where TSignal : IComplexSignal
        {
            OnSignal<TSignal>(Signal, Inverse, Output, 0, 0, Samples);
        }

        /// <summary>
        /// Gets the absolute frequency a sample in the frequency domain data represents.
        /// </summary>
        public static double GetFrequency(int Sample, int NumSamples, double SampleFrequency)
        {
            return (double)Sample * SampleFrequency / (double)NumSamples;
        }

        /// <summary>
        /// Gets the sample in the frequency domain that has the specified frequency.
        /// </summary>
        public static double GetSample(double Frequency, int NumSamples, double SampleFrequency)
        {
            return (double)NumSamples * Frequency / SampleFrequency;
        }
    }

    /// <summary>
    /// A signal of complex numbers.
    /// </summary>
    public interface IComplexSignal
    {
        /// <summary>
        /// Gets the sample at the specified index.
        /// </summary>
        Complex Get(int Index);
    }

    /// <summary>
    /// A complex signal from an array.
    /// </summary>
    public struct ArrayComplexSignal : IComplexSignal
    {
        public ArrayComplexSignal(Complex[] Source)
        {
            this._Source = Source;
        }

        public Complex Get(int Index)
        {
            return this._Source[Index];
        }

        private Complex[] _Source;
    }
}