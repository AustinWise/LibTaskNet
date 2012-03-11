using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Austin.LibTaskNet;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace TestProg
{
    class Program
    {
        private static async Task handleRequest(HttpListenerContext ctx)
        {
            ctx.Response.ContentType = "text/plain";
            var os = ctx.Response.OutputStream;
            var bytes = Encoding.ASCII.GetBytes("Hello World");
            await os.WriteAsync(bytes, 0, bytes.Length);
            ctx.Response.Close();
        }

        private static async Task httpServer()
        {
            CoopScheduler.AddTask(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000);
                    Console.WriteLine("waiting");
                }
            });

            HttpListener l = new HttpListener();
            l.Prefixes.Add("http://+:8080/");
            l.Start();
            while (true)
            {
                var ctx = await l.GetContextAsync();
                CoopScheduler.AddTask(() => handleRequest(ctx));
            }
        }

        static void Main(string[] args)
        {
            CoopScheduler.AddTask(httpServer);
            CoopScheduler.StartScheduler();
        }
    }
}
