using System;

namespace dotnet_eureka
{
    class Program
    {
        const string EUREKA_URL = "";
        const string VIP = "";

        static void Main(string[] args)
        {
            IEurekaDiscovery eureka = new EurekaDiscovery.Builder()
                .SetEurekaUrl(EUREKA_URL)
                .SetWarnLogger((f, a) => { Console.WriteLine(f, a); })
                .Build();

            Console.WriteLine("Eureka lookup {0}", VIP);
            var app = eureka.Lookup(VIP);

            for(int i = 0; i < 20; i++)
                Console.WriteLine(app?.GetNextAppInstance()?.UrlString);
        }
    }
}
