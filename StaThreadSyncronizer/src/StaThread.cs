//  
// Copyright (c) Luis Serra. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.  
//  
using System.Threading;

namespace StaThreadSyncronizer
{
    internal class StaThread
    {
        private Thread mStaThread;
        private IQueueReader<SendOrPostCallbackItem> mQueueConsumer;
        private int mThreadID;
        private ManualResetEvent mStopEvent = new ManualResetEvent(false);

        /// <summary>
        /// This class takes an interface of type IQueueReader, this is really our blocking queue.
        /// The thread is being setup as an STA thread.
        /// </summary>
        /// <param name="reader"></param>
        internal StaThread(IQueueReader<SendOrPostCallbackItem> reader)
        {
            mQueueConsumer = reader;
            mStaThread = new Thread(Run);
            mStaThread.Name = "STA Worker Thread";
            mStaThread.SetApartmentState(ApartmentState.STA);
        }

        /// <summary>
        /// Thread where the code is running
        /// </summary>
        internal int ManagedThreadId
        {
            get
            {
                return mThreadID;
            }
        }

        internal void Start()
        {
            mStaThread.Start();
        }

        /// <summary>
        /// Joint the STA thread to local thread
        /// </summary>
        internal void Join()
        {
            mStaThread.Join();
        }

        /// <summary>
        /// Executing any work items on the Run method means executing them on the STA thread.
        /// </summary>
        private void Run()
        {
            mThreadID = Thread.CurrentThread.ManagedThreadId;
            while (true)
            {
                bool stop = mStopEvent.WaitOne(0);
                if (stop)
                {
                    break;
                }

                SendOrPostCallbackItem workItem = mQueueConsumer.Dequeue();
                if (workItem != null)
                    workItem.Execute();
            }
        }

        internal void Stop()
        {
            mStopEvent.Set();
            mQueueConsumer.ReleaseReader();
            mStaThread.Join();
            mQueueConsumer.Dispose();
        }
    }
}
