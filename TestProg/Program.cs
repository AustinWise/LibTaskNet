using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Austin.LibTaskNet;

namespace TestProg
{
    class Program
    {
        private static void startFun(object arg)
        {
            Task.Create(_ => Console.WriteLine(1), null);
            Task.Create(_ => Console.WriteLine(1), null);
            Console.WriteLine("start fun");
            Task.Yield();
            Console.WriteLine("start fun2");
            Task.Yield();
        }
        static void Main(string[] args)
        {
            Task.TaskMain(startFun, null);
            Console.WriteLine("back to main");
        }
    }
}
