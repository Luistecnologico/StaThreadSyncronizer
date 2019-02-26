//  
// Copyright (c) Luis Serra. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.  
//  
using System;
using System.Security.Permissions;
using System.Threading;

namespace StaThreadSyncronizer
{
    /// <summary>
    /// It is responsible to marshal code into an STA thread, allowing the caller to execute COM APIs that must be on an STA thread.
    /// </summary>
    [SecurityPermission(SecurityAction.Demand, ControlThread = true)]
    public class StaSynchronizationContext : SynchronizationContext, IDisposable
    {
        private BlockingQueue<SendOrPostCallbackItem> mQueue;
        private StaThread mStaThread;

        public StaSynchronizationContext()
           : base()
        {
            mQueue = new BlockingQueue<SendOrPostCallbackItem>();
            mStaThread = new StaThread(mQueue);
            mStaThread.Start();
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            // to avoid deadlock!
            if (Thread.CurrentThread.ManagedThreadId == mStaThread.ManagedThreadId)
            {
                d(state);
                return;
            }

            // create an item for execution
            SendOrPostCallbackItem item = new SendOrPostCallbackItem(d, state, ExecutionType.Send);
            // queue the item
            mQueue.Enqueue(item);
            // wait for the item execution to end
            item.ExecutionCompleteWaitHandle.WaitOne();

            // if there was an exception, throw it on the caller thread, not the
            // sta thread.
            if (item.ExecutedWithException)
                throw item.Exception;
        }

        public void Send(SendOrPostCallback d, object state, int milisecondsTimeout)
        {
            // to avoid deadlock!
            if (Thread.CurrentThread.ManagedThreadId == mStaThread.ManagedThreadId)
            {
                d(state);
                return;
            }

            // create an item for execution
            SendOrPostCallbackItem item = new SendOrPostCallbackItem(d, state, ExecutionType.Send);
            // queue the item
            mQueue.Enqueue(item);
            // wait for the item execution to end
            if (!item.ExecutionCompleteWaitHandle.WaitOne(milisecondsTimeout))
                mQueue.Dequeue(item);

            // if there was an exception, throw it on the caller thread, not the
            // sta thread.
            if (item.ExecutedWithException)
                throw item.Exception;
        }

        /// <summary>
        /// A lambda Action and the maximum waiting time in the queue are passed to it by parameter.
        /// </summary>
        /// <param name="action">Action passed as a lambda expression</param>
        /// <param name="milisecondsTimeout">Blocks the current thread until the current WaitHandle receives a signal, using a 32-bit signed integer to specify the time interval in milliseconds.</param>
        public void Send(Action action, int milisecondsTimeout)
        {
            // Dispatches an asynchronous message to context
            Send(new SendOrPostCallback(_ => action()), null, milisecondsTimeout);
        }

        /// <summary>
        /// A lambda Action is passed to it by parameter. there isn't limited of waiting time.
        /// </summary>
        /// <param name="action">Action passed as a lambda expression</param>
        public void Send(Action action)
        {
            // Dispatches an asynchronous message to context
            this.Send(new SendOrPostCallback(_ => action()), null);
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            // queue the item and don't wait for its execution. This is risky because
            // an unhandled exception will terminate the STA thread. Use with caution.
            SendOrPostCallbackItem item = new SendOrPostCallbackItem(d, state, ExecutionType.Post);
            mQueue.Enqueue(item);
        }

        /// <summary>
        /// not a waiting call, so all we do is queue the item and we are not waiting for the delegate execution to finish.
        /// </summary>
        /// <param name="action">Action passed as a lambda expression</param>
        public void Post(Action action)
        {
            // Dispatches an asynchronous message to context
            Post(new SendOrPostCallback(_ => action()), null);
        }

        public void Dispose()
        {
            mStaThread.Stop();

        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }

    }
}