using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fibers;
using System.Threading;

namespace Austin.LibTaskNet
{
    internal class InternalTask : Fiber
    {
        private static int taskidgen = 0;

        private Action<object> fun;
        private object arg;

        public bool IsReady { get; set; }
        public bool IsSystem { get; set; }
        public bool IsExiting { get; set; }
        public string State { get; set; }
        public readonly int Id;

        public bool WaitDone { get; set; }

        public InternalTask(Action<object> fun, object arg)
        {
            this.fun = fun;
            this.arg = arg;
            this.Id = Interlocked.Increment(ref taskidgen);

            IsSystem = false;
            IsExiting = false;
            IsReady = false;
            State = "created";
        }

        protected override void Run()
        {
            fun(arg);
            this.IsExiting = true;
        }

        public void Yield()
        {
            this.Yield(null);
        }
    }
}
