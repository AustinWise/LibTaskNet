using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Austin.LibTaskNet
{
    public static class CoopScheduler
    {
        private static InternalTask taskrunning = null;
        private static List<InternalTask> alltask = new List<InternalTask>();
        private static int tasknswitch = 0;
        private static Queue<InternalTask> taskrunqueue = new Queue<InternalTask>();

        internal static InternalTask Self
        {
            get
            {
                return taskrunning;
            }
        }

        /// <summary>
        /// Creates a new task and schedules it to run.
        /// </summary>
        /// <param name="fun">The function to schedule.</param>
        /// <returns>The id of the task.</returns>
        public static int Create(Action fun)
        {
            InternalTask t = new InternalTask(fun);
            alltask.Add(t);
            Ready(t);
            return t.Id;
        }

        /// <summary>
        /// Creates a new task and schedules it to run.
        /// </summary>
        /// <param name="fun">The function to schedule.</param>
        /// <param name="arg">The argument to pass to pass to the function.</param>
        /// <returns>The id of the task.</returns>
        public static int Create<T>(Action<T> fun, T arg)
        {
            return Create(() => fun(arg));
        }

        /// <summary>
        /// Adds the task to the run queue.
        /// </summary>
        /// <param name="t">The task.</param>
        internal static void Ready(InternalTask t)
        {
            t.IsReady = true;
            taskrunqueue.Enqueue(t);
        }

        /// <summary>
        /// Yields control to other tasks and reschedules this task.
        /// </summary>
        /// <returns>The number of tasks that ran before returning to this one.</returns>
        public static int Yield()
        {
            int n = tasknswitch;
            Ready(taskrunning);
            Switch();

            return tasknswitch - n - 1;
        }

        /// <summary>
        /// Exits the current thread.
        /// </summary>
        public static void Exit()
        {
            taskrunning.IsExiting = true;
            Switch();
        }

        /// <summary>
        /// Marks the current task as a system task.  This means that this task will not be counted to see if the schduler
        /// should exit when there are no more tasks.
        /// </summary>
        public static void System()
        {
            taskrunning.IsSystem = true;
        }


        /// <summary>
        /// Switches to a different thread without rescheduling this one.
        /// </summary>
        public static void Switch()
        {
            taskrunning.Yield();
        }

        /// <summary>
        /// Sets the current state of this thread.
        /// </summary>
        /// <param name="state">The state of this thread.</param>
        public static void State(string state)
        {
            taskrunning.State = state;
        }

        private static void TaskScheduler()
        {
            while (true)
            {
                if (!alltask.Where(tt => !tt.IsSystem).Any())
                    return;

                if (taskrunqueue.Count == 0)
                    throw new Exception(string.Format("No runnable tasks, %d tasks stalled.", alltask.Count));

                var t = taskrunqueue.Dequeue();

                t.IsReady = false;
                taskrunning = t;
                Interlocked.Increment(ref tasknswitch);

                t.Resume();
                taskrunning = null;

                if (t.IsExiting || t.GetException() != null)
                {
                    t.Dispose();
                    alltask.Remove(t);
                }
            }
        }

        /// <summary>
        /// Starts the scheduler.
        /// </summary>
        /// <param name="fun">The first task to run.</param>
        public static void TaskMain(Action fun)
        {
            Create(fun);
            TaskScheduler();
        }

        /// <summary>
        /// Starts the scheduler.
        /// </summary>
        /// <param name="fun">The first task to run.</param>
        /// <param name="arg">The paramter to the task.</param>
        public static void TaskMain(Action<object> fun, object arg)
        {
            Create(fun, arg);
            TaskScheduler();
        }
    }
}
