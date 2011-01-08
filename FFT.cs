using System;
using System.Collections.Generic;

namespace MD
{
    /// <summary>
    /// Contains functions for performing fourier transforms.
    /// </summary>
    public static class FFT
    {
        private static void _OnArray(bool Inverse, Complex[] Input, Complex[] Output, int InputOffset, int OutputOffset, int Samples, int Step)
        {
            if (Samples == 1)
            {
                Output[OutputOffset] = Input[InputOffset];
            }
            else
            {
                int hsamps = (Samples / 2);
                int dstep = Step * 2;
                _OnArray(Inverse, Input, Output, InputOffset, OutputOffset, hsamps, dstep);
                _OnArray(Inverse, Input, Output, InputOffset + Step, OutputOffset + hsamps, hsamps, dstep);
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
            _OnArray(Inverse, Input, Output, 0, 0, Samples, 1);
            if (Inverse)
            {
                double d = 1.0 / (double)Samples;
                for (int t = 0; t < Output.Length; t++)
                {
                    Output[t] *= d;
                }
            }
        }

        /// <summary>
        /// Performs a fourier transform on the input data. Samples must be a power of two.
        /// </summary>
        public static void OnArray(bool Inverse, Complex[] Input, Complex[] Output, int Samples)
        {
            OnArray(Inverse, Input, Output, 0, 0, Samples);
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
}