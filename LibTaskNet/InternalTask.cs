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

        private Action fun;

        public bool IsReady { get; set; }
        public bool IsSystem { get; set; }
        public bool IsExiting { get; set; }
        public string State { get; set; }
        public readonly int Id;

        public bool WaitDone { get; set; }

        public InternalTask(Action fun)
        {
            this.fun = fun;
            this.Id = Interlocked.Increment(ref taskidgen);

            IsSystem = false;
            IsExiting = false;
            IsReady = false;
            State = "created";
        }

        protected override void Run()
        {
            fun();
            this.IsExiting = true;
        }

        public void Yield()
        {
            this.Yield(null);
        }
    }
}
