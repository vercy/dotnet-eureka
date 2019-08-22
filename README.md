# dotnet-eureka

Roll-your own Eureka client with no package dependencies. It uses TcpClient and streams so that connection specific timeouts and headers can be used without the overhead of the HttpClient.

##Knobs
* **CacheExpiration** the time between refreshes per VIP. Default: 30 seconds
* **Logger** Simplicistic logger interface allowing for the use of your favorite tool. Default: noop
