using System;

namespace dotnet_eureka
{
    class Program
    {
        const string EUREKA_URL = "";
        const string VIP = "";

        static void Main(string[] args)
        {
            IEurekaDiscovery eureka = new EurekaDiscoveryBuilder()
                .SetEurekaUrl(EUREKA_URL)
                .Build();

            var app = eureka.Lookup(VIP);
            
            Console.WriteLine("Eureka node for vip=" + VIP + " url=" + app?.GetNextAppInstance()?.UrlString);
        }
    }
}
