using System;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;

namespace Repro.Tests
{
    /// <summary>
    /// handles a singleton test server -- named test server instance to avoid naming collision with the testserver that is created here..
    /// </summary>
    public class TestServerInstance
    {
        private static object locker = new object();
        private static TestServer TestServer;

        public static TestServer Instance
        {
            get
            {
                //just to make sure two tests don't try to initialize at the same time..
                lock (locker)
                {
                    if (TestServer == null)
                    {                        
                        TestServer = new TestServer(new WebHostBuilder().UseContentRoot(System.IO.Directory.GetCurrentDirectory()).UseStartup<Startup>());                        
                    }

                    return TestServer;
                }
            }
        }

        





    }
}
