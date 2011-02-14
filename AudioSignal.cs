using System;
using System.Collections.Generic;

namespace MD
{
    /// <summary>
    /// A representation of a signal that allows it to be played as audio.
    /// </summary>
    public abstract class AudioSignal<TSample>
        where TSample : IAudioSample
    {
        /// <summary>
        /// Gets the amount of samples played in a second for this signal.
        /// </summary>
        public abstract double SampleRate { get; }

        /// <summary>
        /// Gets a multi-channel discrete signal for the audio.
        /// </summary>
        public abstract MultiDiscreteSignal<TSample> Samples { get; }

        /// <summary>
        /// Gets a multi-channel discrete signal of double values for the audio.
        /// </summary>
        public virtual MultiDiscreteSignal<double> DoubleSamples
        {
            get
            {
                // Default implementation of this is horribly inefficent
                return new _DoubleSignal<MultiDiscreteSignal<TSample>>(this.Samples);
            }
        }

        private class _DoubleSignal<TSource> : MultiDiscreteSignal<double>
            where TSource : MultiDiscreteSignal<TSample>
        {
            public _DoubleSignal(TSource Source)
            {
                this._Source = Source;
            }

            public override int Channels
            {
                get
                {
                    return this._Source.Channels;
                }
            }

            public override int Size
            {
                get
                {
                    return this._Source.Size;
                }
            }

            public override double[] Read(int Index)
            {
                TSample[] samps = this._Source.Read(Index);
                double[] dsamps = new double[samps.Length];
                for (int t = 0; t < dsamps.Length; t++)
                {
                    dsamps[t] = samps[t].Value;
                }
                return dsamps;
            }

            private TSource _Source;
        }
    }

    /// <summary>
    /// Represents a single sample of audio.
    /// </summary>
    public interface IAudioSample
    {
        /// <summary>
        /// Get the double (most precise) value of this sample, from -1.0 to 1.0.
        /// </summary>
        double Value { get; }
    }
}