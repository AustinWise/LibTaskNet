LibTaskNet, a libtask style coroutine library for .NET
------------------------------------------------------

LibTaskNet is a coroutine library inspired by [libtask].  Originally intented to
have an API similar to that of libtask, LibTaskNet now uses the new C# await
keyword to enable switching between functions.  Instead of providing IO
functions, the new .NET 4.5 Async methods can be used.  This enables a
co-routine that is supported by standard .NET APIs.  Viewed another way, you can
write Node.JS style apps without the layers of nested callback function
definitions.

Check the [fibers] branch for a version of this library that uses fibers and is
much more like [libtask].

Example
-------
A super-simple HTTP server that processes each request in its own coroutine and
every second prints "waiting".

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

Dependencies
------------

 - .NET Framework 4.5

Credits
-------
This work is based in large part on the blog entry [Await, SynchronizationContext, and Console Apps]
by Stephen Toub.

License
-------

LibTaskNet is licensed under the three-clause BSD license.

  [libtask]: http://swtch.com/libtask/
  [Await, SynchronizationContext, and Console Apps]: http://blogs.msdn.com/b/pfxteam/archive/2012/01/20/10259049.aspx
  [fibers]: https://github.com/AustinWise/LibTaskNet/tree/fibers
