using System;
using System.Collections.Generic;

namespace MD
{
    /// <summary>
    /// A finite collection of uniformly-spaced data samples.
    /// </summary>
    public interface IDiscreteSignal<TSample>
    {
        /// <summary>
        /// Gets the sample at the specified positive index.
        /// </summary>
        TSample Get(int Index);

        /// <summary>
        /// Gets the amount of samples in the signal.
        /// </summary>
        int Size { get; }
    }

    /// <summary>
    /// A discrete signal that can be written to an array more efficently than taking individual samples.
    /// </summary>
    public interface IWritableDiscreteSignal<TSample> : IDiscreteSignal<TSample>
    {
        /// <summary>
        /// Writes a portion of the signal to the specified ouput.
        /// </summary>
        void Write(int Index, int Amount, TSample[] Output);
    }

    /// <summary>
    /// A discrete signal where a section of samples with a high likelyhood of being read can be made.
    /// </summary>
    public interface ISectionableDiscreteSignal<TSample> : IDiscreteSignal<TSample>
    {
        /// <summary>
        /// Gets a signal that can be efficently read from for the specified section of this signal.
        /// </summary>
        IDiscreteSignal<TSample> GetSection(int Index, int Amount);
    }
}