using System;
using System.Threading;

namespace dotnet_eureka
{
    class Program
    {
        const string EUREKA_URL = "";         const string VIP = "";

        static void Main(string[] args)
        {
            IEurekaDiscovery eureka = new EurekaDiscovery.Builder()
                .SetEurekaUrl(EUREKA_URL)
                .SetCacheExiration(TimeSpan.FromSeconds(5))
                .SetLogger(new ColoredConsoleLogger())
                .Build();

            Console.WriteLine("Eureka lookup {0}", VIP);
            for (int i = 0; i < 20; i++)
            {
                var app = eureka.Lookup(VIP);
                Console.WriteLine(app?.GetNextAppInstance()?.UrlString);
                Thread.Sleep(1000);
            }
        }
    }

    class ColoredConsoleLogger : EurekaDiscovery.ILogger
    {
        public void Warn(string f, params object[] a) {
            Console.WriteLine("\u001B[31m" + string.Format(f, a) + "\u001b[0m");
        }
        public void Debug(string f, params object[] a) {
            Console.WriteLine("\u001B[33m" + string.Format(f, a) + "\u001b[0m");
        }
    }
}
