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
        }
    }
}
