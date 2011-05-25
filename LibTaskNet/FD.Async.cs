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

        public static void WaitAsyncPattern(Func<AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            var ar = begin(null, null);
            Wait(new AsyncPatternTask(ar));
            end(ar);
        }

        public static TResult WaitAsyncPattern<T1, T2, T3, TResult>(Func<T1, T2, T3, AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end, T1 arg1, T2 arg2, T3 arg3)
        {
            var ar = begin(arg1, arg2, arg3, null, null);
            Wait(new AsyncPatternTask(ar));
            return end(ar);
        }

        public static int Read(Stream s, byte[] buffer, int offset, int count)
        {
            return WaitAsyncPattern(s.BeginRead, s.EndRead, buffer, offset, count);
        }
    }
}
