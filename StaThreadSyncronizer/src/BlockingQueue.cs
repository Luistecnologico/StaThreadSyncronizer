//  
// Copyright (c) Luis Serra. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.  
//  
using System;
using System.Collections.Generic;
using System.Threading;

namespace StaThreadSyncronizer
{
    internal interface IQueueReader<T> : IDisposable
    {
        T Dequeue();
        void ReleaseReader();
    }

    internal interface IQueueWriter<T> : IDisposable
    {
        void Enqueue(T data);
    }

    /// <summary>
    /// Queue to queue up work items from thread X to my STA thread
    /// and dequeue items only when there are items in the queue.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class BlockingQueue<T> : IQueueReader<T>, IQueueWriter<T>, IDisposable
    {
        // use a .NET List (not Queue) to store the data
        private List<T> mQueue = new List<T>();
        // create a semaphore that contains the items in the queue as resources.
        // initialize the semaphore to zero available resources (empty queue).
        private Semaphore mSemaphore = new Semaphore(0, int.MaxValue);
        // a event that gets triggered when the reader thread is exiting
        private ManualResetEvent mKillThread = new ManualResetEvent(false);
        // wait handles that are used to unblock a Dequeue operation.
        // Either when there is an item in the queue
        // or when the reader thread is exiting.
        private WaitHandle[] mWaitHandles;

        /// <summary>
        /// Constructor of Blocking Queue
        /// </summary>
        public BlockingQueue()
        {
            mWaitHandles = new WaitHandle[2] { mSemaphore, mKillThread };
        }

        /// <summary>
        /// add an available resource to the semaphore,
        /// because we just put an item
        /// into the queue.
        /// </summary>
        /// <param name="data">Item that will be stored into the Queue</param>
        public void Enqueue(T data)
        {
            lock (mQueue) mQueue.Add(data);
            // add an available resource to the semaphore,
            // because we just put an item
            // into the queue.
            mSemaphore.Release();
        }

        /// <summary>
        /// Wait until something pops into the queue and return the Next Item to launch.
        /// </summary>
        /// <returns>return Next SendOrPostCallbackItem</returns>
        public T Dequeue()
        {
            // wait until there is an item in the queue
            WaitHandle.WaitAny(mWaitHandles);
            lock (mQueue)
            {
                if (mQueue.Count > 0)
                {
                    T mQueueTwo = mQueue[0];
                    mQueue.RemoveAt(0);
                    return mQueueTwo;
                }
            }
            return default(T);
        }

        /// <summary>
        /// Remove from the queue a specific item
        /// </summary>
        /// <param name="item">Item that will be removed</param>
        /// <returns>Return a default T value. This is for congruence within the system.</returns>
        public T Dequeue(T item)
        {
            WaitHandle.WaitAny(mWaitHandles);
            lock (mQueue)
            {
                if (mQueue.Count > 0)
                {
                    mQueue.Remove(item);
                }

            }
            return default(T);
        }

        /// <summary>
        /// Kill (stop) the Reader class (and, so, its threaders) when context class is disposed.
        /// </summary>
        public void ReleaseReader()
        {
            mKillThread.Set();
        }

        /// <summary>
        /// When this class is disposed, close Semaphore (to not accept new requests) and clear the Queue.
        /// </summary>
        void IDisposable.Dispose()
        {
            if (mSemaphore != null)
            {
                mSemaphore.Close();
                mQueue.Clear();
                mSemaphore = null;
            }
        }
    }
}
