using System;
using System.Collections.Generic;

namespace MD
{
    /// <summary>
    /// Contains functions for performing fourier transforms.
    /// </summary>
    public static class FFT
    {
        /// <summary>
        /// Performs a fourier transform on the input data. Samples must be a power of two.
        /// </summary>
        public static void OnArray(bool Inverse, Complex[] Input, Complex[] Output, int InputStart, int OutputStart, int Samples, int Step)
        {
            if (Samples == 1)
            {
                Output[OutputStart] = Input[InputStart];
            }
            else
            {
                int hsamps = (Samples / 2);
                int dstep = Step * 2;
                OnArray(Inverse, Input, Output, InputStart, OutputStart, hsamps, dstep);
                OnArray(Inverse, Input, Output, InputStart + Step, OutputStart + hsamps, hsamps, dstep);
                double c = (Inverse ? 2.0 : -2.0) * Math.PI;
                for (int i = 0; i < hsamps; i++)
                {
                    Complex t = Output[OutputStart + i];
                    Complex e = new Complex(c * (double)i / (double)Samples).TimesI.Exp;
                    Complex es = e * Output[OutputStart + hsamps + i];
                    Output[OutputStart + i] = t + es;
                    Output[OutputStart + hsamps + i] = t - es;
                }
            }
        }

        /// <summary>
        /// Performs a fourier transform on the input data. Samples must be a power of two.
        /// </summary>
        public static void OnArray(bool Inverse, Complex[] Input, Complex[] Output, int Samples)
        {
            OnArray(Inverse, Input, Output, 0, 0, Samples, 1);
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
        /// Gets the absolute frequency a sample in the frequency domain data represents.
        /// </summary>
        public static double AbsoluteFrequency(int Sample, int NumSamples, double SampleFrequency)
        {
            return ((double)Sample / (double)NumSamples) * SampleFrequency;
        }
    }
}