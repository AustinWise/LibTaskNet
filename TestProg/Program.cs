using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Austin.LibTaskNet;
using System.IO;
using System.Net;

namespace TestProg
{
    class Program
    {
        private static void startFun(object arg)
        {
            CoopScheduler.Create(() => Console.WriteLine("lol1"));
            CoopScheduler.Create(() => Console.WriteLine("lol2"));

            var fs = new FileStream(@"d:\Down\121465635.html", FileMode.Open, FileAccess.Read);
            byte[] bytes = new byte[100];

            FD.Read(fs, bytes, 0, bytes.Length);
            Console.WriteLine(Encoding.ASCII.GetString(bytes));
        }

        private static void handleRequest(HttpListenerContext ctx)
        {
            var os = ctx.Response.OutputStream;
            var bytes = Encoding.ASCII.GetBytes("Turtles are the best.");
            FD.Write(os, bytes, 0, bytes.Length);
            ctx.Response.Close();
        }

        private static void httpServer()
        {
            CoopScheduler.Create(() =>
            {
                while (true)
                {
                    FD.Delay(1000);
                    Console.WriteLine("waiting");
                }
            });

            HttpListener l = new HttpListener();
            l.Prefixes.Add("http://+:1337/");
            l.Start();
            while (true)
            {
                var ctx = FD.WaitAsyncPattern(l.BeginGetContext, l.EndGetContext);
                CoopScheduler.Create(handleRequest, ctx);
            }
        }

        static void Main(string[] args)
        {
            CoopScheduler.TaskMain(httpServer);
            Console.WriteLine("back to main");
        }
    }
}
