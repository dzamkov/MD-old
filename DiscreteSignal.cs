using System;
using System.Collections.Generic;

namespace MD
{
    /// <summary>
    /// A static, finite, ordered collection of data.
    /// </summary>
    /// <typeparam name="TSample">The type of an element in the signal.</typeparam>
    public abstract class DiscreteSignal<TSample>
    {
        /// <summary>
        /// Gets the size of this signal in samples.
        /// </summary>
        public abstract int Size { get; }

        /// <summary>
        /// Reads a sample from the specified index of this signal.
        /// </summary>
        public abstract TSample Read(int Index);

        /// <summary>
        /// Gets a feed for this signal at the specified offset.
        /// </summary>
        public virtual DiscreteFeed<TSample> GetFeed(int Offset)
        {
            return DiscreteFeed<TSample>.Create<DiscreteSignal<TSample>>(this, Offset);
        }
    }

    /// <summary>
    /// A discrete signal with multiple related channels.
    /// </summary>
    public abstract class MultiDiscreteSignal<TSample> : DiscreteSignal<TSample[]>
    {
        /// <summary>
        /// Gets the amount of channels in this signal.
        /// </summary>
        public abstract int Channels { get; }

        /// <summary>
        /// Gets a signal for one of the channels in this signal.
        /// </summary>
        public virtual DiscreteSignal<TSample> GetChannel(int Channel)
        {
            return new _Channel<MultiDiscreteSignal<TSample>>(this, Channel);
        }

        private class _Channel<TSignal> : DiscreteSignal<TSample>
            where TSignal : MultiDiscreteSignal<TSample>
        {
            public _Channel(TSignal Source, int Channel)
            {
                this._Source = Source;
                this._ChannelIndex = Channel;
            }

            public override int Size
            {
                get
                {
                    return this._Source.Size;
                }
            }

            public override TSample Read(int Index)
            {
                return this._Source.Read(Index)[this._ChannelIndex];
            }

            private int _ChannelIndex;
            private TSignal _Source;
        }
    }

    /// <summary>
    /// A representation of a discrete signal that can only be read in one direction.
    /// </summary>
    public abstract class DiscreteFeed<TSample>
    {
        /// <summary>
        /// Returns true and reads a sample from the data, or returns false to indicate no more data
        /// to read.
        /// </summary>
        public abstract bool Read(ref TSample Data);

        /// <summary>
        /// Reads data to an array. Returns how many samples were read, which will only be lower than amount
        /// if the given section intersects the end of the feed.
        /// </summary>
        public virtual int Read(TSample[] Data, int Offset, int Amount)
        {
            int amountread = 0;
            TSample samp = default(TSample);
            while (Amount > 0 && this.Read(ref samp))
            {
                Data[Offset] = samp;
                Amount--;
                Offset++;
                amountread++;
            }
            return amountread;
        }

        /// <summary>
        /// Creates a feed by reading data from the given signal.
        /// </summary>
        public static DiscreteFeed<TSample> Create<TSignal>(TSignal Signal, int Offset)
            where TSignal : DiscreteSignal<TSample>
        {
            return new _SignalFeed<TSignal>(Signal, Offset);
        }

        private class _SignalFeed<TSignal> : DiscreteFeed<TSample>
            where TSignal : DiscreteSignal<TSample>
        {
            public _SignalFeed(TSignal Source, int Offset)
            {
                this._Source = Source;
                this._Offset = Offset;
            }

            public override bool Read(ref TSample Data)
            {
                if (this._Offset < this._Source.Size)
                {
                    Data = this._Source.Read(this._Offset);
                    this._Offset++;
                    return true;
                }
                return false;
            }

            private int _Offset;
            private TSignal _Source;
        }
    }
}