using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Austin.LibTaskNet
{
    public static class CoopScheduler
    {
        private static BlockingCollection<Tuple<SendOrPostCallback, object>> sCallbacks = new BlockingCollection<Tuple<SendOrPostCallback, object>>();
        private static int sOutstandingTasks = 0;

        private static void CompleteTask(Task t)
        {
            //observe the result on the main thread, which will propegate execeptions
            sCallbacks.Add(new Tuple<SendOrPostCallback, object>(_ => t.GetAwaiter().GetResult(), null));
            CompleteTask();
        }

        private static void StartTask()
        {
            Interlocked.Increment(ref sOutstandingTasks);
        }

        private static void CompleteTask()
        {
            if (Interlocked.Decrement(ref sOutstandingTasks) == 0)
                sCallbacks.CompleteAdding();
        }

        /// <summary>
        /// Creates a new task and schedules it to run.
        /// </summary>
        /// <param name="fun">The function to schedule.</param>
        public static void AddTask(Func<Task> task)
        {
            StartTask();
            sCallbacks.Add(new Tuple<SendOrPostCallback, object>(_ => task().ContinueWith(CompleteTask), null));
        }

        /// <summary>
        /// Starts executing tasks and does not return until all tasks have
        /// completed execution.
        /// </summary>
        /// <remarks>
        /// If a scheduled task throws an exception, it will be raised here.
        /// If no tasks are scheduled, an exception will be thrown.
        /// </remarks>
        public static void StartScheduler()
        {
            if (sOutstandingTasks == 0)
                throw new InvalidOperationException("At least one task should be added with AddTask() before calling StartSched().");
            using (new CoopSyncContext())
            {
                Tuple<SendOrPostCallback, object> tup;
                while (sCallbacks.TryTake(out tup, Timeout.Infinite))
                    tup.Item1(tup.Item2);
            }
        }

        class CoopSyncContext : SynchronizationContext, IDisposable
        {
            private SynchronizationContext mOldContext;

            public CoopSyncContext()
            {
                this.mOldContext = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(this);
            }

            public void Dispose()
            {
                SynchronizationContext.SetSynchronizationContext(mOldContext);
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                sCallbacks.Add(new Tuple<SendOrPostCallback, object>(d, state));
            }

            public override void OperationStarted()
            {
                StartTask();
            }

            public override void OperationCompleted()
            {
                CompleteTask();
            }
        }
    }
}
