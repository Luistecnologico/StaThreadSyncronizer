//  
// Copyright (c) Luis Serra. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.  
//  
using System;
using System.Threading;

namespace StaThreadSyncronizer
{
    /// <summary>
    /// Executing the delegate in two possible modes: Send and Post
    /// </summary>
    internal enum ExecutionType
    {
        Post,
        Send
    }

    /// <summary>
    /// Contains the delegate we wish to execute on the STA thread
    /// </summary>
    internal class SendOrPostCallbackItem
    {
        object mState;
        private ExecutionType mExeType;
        SendOrPostCallback mMethod;
        ManualResetEvent mAsyncWaitHandle = new ManualResetEvent(false);
        Exception mException = null;

        /// <summary>
        /// Delegate we wish to execute
        /// </summary>
        /// <param name="callback">To do method</param>
        /// <param name="state">Parameters of the over method</param>
        /// <param name="type">Delegate running mode: Send or Post</param>
        internal SendOrPostCallbackItem(SendOrPostCallback callback, object state, ExecutionType type)
        {
            mMethod = callback;
            mState = state;
            mExeType = type;
        }

        internal Exception Exception
        {
            get { return mException; }
        }

        internal bool ExecutedWithException
        {
            get { return mException != null; }
        }

        /// <summary>
        /// this code must run ont the STA thread
        /// </summary>
        internal void Execute()
        {
            if (mExeType == ExecutionType.Send)
                Send();
            else
                Post();
        }

        /// <summary>
        /// calling thread will block until mAsyncWaitHanel is set
        /// </summary>
        internal void Send()
        {
            try
            {
                //call the thread
                mMethod(mState);
            }
            catch (Exception e)
            {
                mException = e;
            }
            finally
            {
                mAsyncWaitHandle.Set();
            }
        }

        /// <summary>
        /// It just calls the method, no need to notify when it is done, and there is no need to track the exception either.
        /// <excepction>Unhandle execptions will terminate the STA thread</excepction>
        /// </summary>
        internal void Post()
        {
            mMethod(mState);
        }

        internal WaitHandle ExecutionCompleteWaitHandle
        {
            get { return mAsyncWaitHandle; }
        }
    }
}
