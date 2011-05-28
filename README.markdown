LibTaskNet, a libtask style coroutine library for .NET
------------------------------------------------------

LibTaskNet is a coroutine library inspired by [libtask].  Has nice methods for
using existing .NET asynchronous APIs with coroutines.

Example
-------
A super-simple HTTP server that processes each request in its own coroutine and
every second prints "waiting".

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

Dependencies
------------

 - .NET Framework 4.0 (Can also be compiled .NET 3.5)

License
-------

LibTaskNet is licensed under the three-clause BSD license.  However the
Fibers.cpp from the [CoroutinesNET] MSDN article has it's own EULA.  The EULA
looks reasonably permissive.


Currently limitations
---------------------

* Probably only works with x86.  It should not be too hard to make there
  at least be a build option for x64 though.


Areas for improvement
---------------------

 - Better reporting of exceptions from coroutines.  Currently they just get
   eaten silently by the Fiber class.
 - Use the new (non-deprecated) .NET hosting API.
 - Perhaps integrate with the TaskScheduler API.  This probably would requiring
   interfacing with the new .NET hosting API as users of the Task class assume
   they can block and other tasks will continue to execute.  Implementing
   [IHostSyncManager] would probably be the right place to address this problem.
 - Replace Fibers.cpp with something that has a more free license.


  [CoroutinesNET]: http://msdn.microsoft.com/en-us/magazine/cc164086.aspx
  [IHostSyncManager]: http://msdn.microsoft.com/en-us/library/ms164542.aspx
  [libtask]: http://swtch.com/libtask/