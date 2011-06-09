using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace Austin.LibTaskNet
{
    partial class FD
    {
        private class AsyncPatternTask : FdTask
        {
            public AsyncPatternTask(IAsyncResult ar)
            {
                this.ar = ar;
            }

            private IAsyncResult ar;

            public override bool IsDone
            {
                get { return ar.IsCompleted; }
            }

            private WaitHandle waitEvent;
            public override WaitHandle Event
            {
                get
                {
                    if (waitEvent == null)
                        waitEvent = ar.AsyncWaitHandle;
                    return waitEvent;
                }
            }

            public override void Dispose()
            {
                if (waitEvent != null)
                    waitEvent.Close();
            }
        }

        #region WaitAsyncPattern
        public static TResult WaitAsyncPattern<TResult>(Func<AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end)
        {
            var ar = begin(null, null);
            Wait(new AsyncPatternTask(ar));
            return end(ar);
        }

        public static TResult WaitAsyncPattern<T1, TResult>(Func<T1, AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end, T1 arg1)
        {
            var ar = begin(arg1, null, null);
            Wait(new AsyncPatternTask(ar));
            return end(ar);
        }

        public static TResult WaitAsyncPattern<T1, T2, TResult>(Func<T1, T2, AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end, T1 arg1, T2 arg2)
        {
            var ar = begin(arg1, arg2, null, null);
            Wait(new AsyncPatternTask(ar));
            return end(ar);
        }

        public static TResult WaitAsyncPattern<T1, T2, T3, TResult>(Func<T1, T2, T3, AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end, T1 arg1, T2 arg2, T3 arg3)
        {
            var ar = begin(arg1, arg2, arg3, null, null);
            Wait(new AsyncPatternTask(ar));
            return end(ar);
        }

        public static void WaitAsyncPatternUnit(Func<AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            var ar = begin(null, null);
            Wait(new AsyncPatternTask(ar));
            end(ar);
        }

        public static void WaitAsyncPatternUnit<T1>(Func<T1, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end, T1 arg1)
        {
            var ar = begin(arg1, null, null);
            Wait(new AsyncPatternTask(ar));
            end(ar);
        }

        public static void WaitAsyncPatternUnit<T1, T2>(Func<T1, T2, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end, T1 arg1, T2 arg2)
        {
            var ar = begin(arg1, arg2, null, null);
            Wait(new AsyncPatternTask(ar));
            end(ar);
        }

        public static void WaitAsyncPatternUnit<T1, T2, T3>(Func<T1, T2, T3, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end, T1 arg1, T2 arg2, T3 arg3)
        {
            var ar = begin(arg1, arg2, arg3, null, null);
            Wait(new AsyncPatternTask(ar));
            end(ar);
        }
        #endregion

        private static void WaitTask(System.Threading.Tasks.Task t)
        {
            if (t.Status == System.Threading.Tasks.TaskStatus.Created)
                t.Start();
            Wait(new AsyncPatternTask(t));
            return;
        }

        private static T WaitTask<T>(System.Threading.Tasks.Task<T> t)
        {
            if (t.Status == System.Threading.Tasks.TaskStatus.Created)
                t.Start();
            Wait(new AsyncPatternTask(t));
            return t.Result;
        }

        /// <summary>
        /// An asynchronous version of the Stream.Read method.
        /// </summary>
        public static int Read(Stream s, byte[] buffer, int offset, int count)
        {
            return WaitAsyncPattern(s.BeginRead, s.EndRead, buffer, offset, count);
        }

        /// <summary>
        /// An asynchronous version of the Stream.Write method.
        /// </summary>
        public static void Write(Stream s, byte[] buffer, int offset, int count)
        {
            WaitAsyncPatternUnit(s.BeginWrite, s.EndWrite, buffer, offset, count);
        }
    }
}
