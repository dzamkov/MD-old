using System;
using System.Collections.Generic;
using System.Threading;

namespace MD
{
    /// <summary>
    /// Manages an amount of threads to complete a queue of tasks.
    /// </summary>
    public class ThreadPool
    {
        public ThreadPool(NextTaskHandler NextTask)
        {
            this._NextTask = NextTask;
            this._TargetThreadAmount = 5;
        }

        /// <summary>
        /// Gets the current amount of threads running in the thread pool.
        /// </summary>
        public int ThreadAmount
        {
            get
            {
                return this._ThreadAmount;
            }
        }

        /// <summary>
        /// Gets or sets the maximum amount of threads that can run in the thread pool at a time.
        /// </summary>
        public int TargetThreadAmount
        {
            get
            {
                return this._TargetThreadAmount;
            }
            set
            {
                this._TargetThreadAmount = value;
            }
        }

        /// <summary>
        /// Informs the thread pool that another task has been created.
        /// </summary>
        public void Signal()
        {
            lock (this)
            {
                if (this._ThreadAmount < this._TargetThreadAmount)
                {
                    this._ThreadAmount++;
                    Thread th = new Thread(this._ThreadLoop);
                    th.IsBackground = true;
                    th.Start();
                }
            }
        }

        private void _ThreadLoop()
        {
            while (true)
            {
                Action task = this._NextTask();
                if (task == null)
                {
                    lock (this)
                    {
                        this._ThreadAmount--;
                    }
                    return;
                }
                else
                {
                    task();
                }
            }
        }

        private NextTaskHandler _NextTask;
        private int _ThreadAmount;
        private int _TargetThreadAmount;
    }

    /// <summary>
    /// A handler which gets the next task for a thread pool. Handlers of this type should be thread safe.
    /// </summary>
    public delegate Action NextTaskHandler();
}