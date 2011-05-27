using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Austin.LibTaskNet;
using System.IO;

namespace TestProg
{
    class Program
    {
        private static void startFun(object arg)
        {
            CoopScheduler.Create(_ => Console.WriteLine("lol1"), null);
            CoopScheduler.Create(_ => Console.WriteLine("lol2"), null);

            var fs = new FileStream(@"d:\Down\121465635.html", FileMode.Open, FileAccess.Read);
            byte[] bytes = new byte[100];

            FD.Read(fs, bytes, 0, bytes.Length);
            Console.WriteLine(Encoding.ASCII.GetString(bytes));
        }
        static void Main(string[] args)
        {
            CoopScheduler.TaskMain(startFun, null);
            Console.WriteLine("back to main");
        }
    }
}
