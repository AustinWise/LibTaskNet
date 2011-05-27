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

        public static int Create(Action fun)
        {
            return Create(_ => fun(), null);
        }

        public static int Create(Action<object> fun, object arg)
        {
            InternalTask t = new InternalTask(fun, arg);
            alltask.Add(t);
            Ready(t);
            return t.Id;
        }

        internal static void Ready(InternalTask t)
        {
            t.IsReady = true;
            taskrunqueue.Enqueue(t);
        }

        public static int Yield()
        {
            int n = tasknswitch;
            Ready(taskrunning);
            Switch();

            return tasknswitch - n - 1;
        }

        public static void Exit()
        {
            taskrunning.IsExiting = true;
            Switch();
        }

        public static void System()
        {
            taskrunning.IsSystem = true;
        }

        public static void Switch()
        {
            taskrunning.Yield();
        }

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

                InternalTask t = taskrunqueue.Dequeue();

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

        public static void TaskMain(Action<object> fun, object arg)
        {
            Create(fun, arg);
            TaskScheduler();
        }
    }
}
