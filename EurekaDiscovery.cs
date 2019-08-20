using System;
using System.Collections.Generic;

namespace dotnet_eureka
{
    internal interface IEurekaDiscovery
    {
        EurekaDiscovery.App Lookup(string vip);
    }

    static class EurekaDiscovery
    {
        internal class App
        {
            static readonly Random random = new Random();
            readonly List<Instance> instances;

            App(List<Instance> vips)
            {
                instances = new List<Instance>(vips);
            }

            internal Instance GetNextAppInstance()
            {
                List<Instance> instancesCopy = new List<Instance>(this.instances);
                Shuffle(instancesCopy);

                foreach (Instance instance in instancesCopy)
                    if (instance != null && instance.Status == Status.UP)
                        return instance;

                return null;
            }

            static void Shuffle<T>(IList<T> list)
            {
                int n = list.Count;
                while (n > 1)
                {
                    n--;
                    int k = random.Next(n + 1);
                    T value = list[k];
                    list[k] = list[n];
                    list[n] = value;
                }
            }

            internal enum Status { UP, DOWN, STARTING, OUT_OF_SERVICE, UNKNOWN }

            internal class Instance
            {
                Instance(string urlString, string vipAddress, string status)
                {
                    this.Status = (Status)Enum.Parse(typeof(Status), status);
                    this.UrlString = urlString + vipAddress + "/";
                }

                internal Status Status { get; }
                internal string UrlString { get; }
            }
        }
    }

    
        
    class HttpClient : IEurekaDiscovery
    {

        //private static final Logger LOG = LoggerFactory.getLogger(HttpClient.class);
        //private final String eurekaHost;

        //    IConnectionFactory connectionFactory = urlString-> {
        //        URL url = new URL(urlString);
        //        return (HttpURLConnection) url.openConnection();
        //};

        //@FunctionalInterface
        //    interface IConnectionFactory
        //{
        //    HttpURLConnection getConnection(String urlString) throws IOException;
        //}

        internal HttpClient(String eurekaHost)
        {
            //Preconditions.checkNotNull(eurekaHost, "eurekaHost cannot be null");
            //Preconditions.checkArgument(!eurekaHost.isEmpty(), "eurekaHost cannot be empty");

            //if (eurekaHost.endsWith("/"))
            //{
            //    this.eurekaHost = eurekaHost;
            //}
            //else
            //{
            //    this.eurekaHost = eurekaHost + "/";
            //}
        }

        public EurekaDiscovery.App Lookup(string vip)
        {
            //String url = eurekaHost + "apps/" + vip;
            //HttpURLConnection conn = null;
            //try
            //{
            //    conn = connectionFactory.getConnection(url);
            //    conn.setConnectTimeout(5000);
            //    conn.setReadTimeout(5000);
            //    conn.setRequestProperty("Accept", "application/json");

            //    int responseCode = conn.getResponseCode();
            //    if (responseCode != 200)
            //    {
            //        LOG.warn("lookup {} - Eureka server {} returned HTTP {}. Lookup fails", vip, eurekaHost, responseCode);
            //        return Optional.empty();
            //    }

            //    return Optional.ofNullable(parseJsonResponseFrom(conn));
            //}
            //catch (MalformedURLException e)
            //{
            //    LOG.warn("lookup {} - Invalid Eureka server url {}", vip, url, exceptionOrNull(e));
            //}
            //catch (ConnectException e)
            //{
            //    LOG.warn("lookup {} - Could not connect to eureka server {}", vip, eurekaHost, exceptionOrNull(e));
            //}
            //catch (SocketTimeoutException e)
            //{
            //    LOG.warn("lookup {} - Eureka server {} timed out.", vip, eurekaHost, exceptionOrNull(e));
            //}
            //catch (IOException e)
            //{
            //    LOG.warn("lookup {} - Eureka server {} failed. Cause: {}", vip, eurekaHost, e.getMessage(), exceptionOrNull(e));
            //}
            //finally
            //{
            //    if (conn != null)
            //    {
            //        conn.disconnect();
            //    }
            //}
            //return Optional.empty();
            return null;
        }

        //Throwable exceptionOrNull(Throwable e)
        //{
        //    return LOG.isTraceEnabled() ? e : null;
        //}

        //    App parseJsonResponseFrom(HttpURLConnection conn) throws IOException
        //    {
        //            try (Reader in = new InputStreamReader(conn.getInputStream(), StandardCharsets.UTF_8)) {
        //                return new App(readAppInstances(in));
        //            }
        //        }

        //        private List<App.Instance> readAppInstances(Reader reader)
        //{
        //    JsonReader jsonReader = new JsonReader(reader);
        //    JsonObject response = new JsonParser().parse(jsonReader).getAsJsonObject();
        //    JsonObject application = response.getAsJsonObject("application");
        //    JsonArray instances = application.getAsJsonArray("instance");

        //    List<App.Instance> result = new ArrayList<>();
        //    for (JsonElement instanceElement : instances)
        //    {
        //        JsonObject instanceObject = instanceElement.getAsJsonObject();
        //        result.add(new App.Instance(
        //                instanceObject.get("homePageUrl").getAsString(),
        //                instanceObject.get("vipAddress").getAsString(),
        //                instanceObject.get("status").getAsString()));
        //    }

        //    return result;
        //}
    }

    internal class EurekaDiscoveryBuilder
    {
        string EurekaUrl;
        IEurekaDiscovery HttpClient;


        public EurekaDiscoveryBuilder SetEurekaUrl(string eurekaUrl)
        {
            this.EurekaUrl = eurekaUrl;
            return this;
        }

        public EurekaDiscoveryBuilder SetHttpClient(IEurekaDiscovery httpClient)
        {
            this.HttpClient = httpClient;
            return this;
        }

        public IEurekaDiscovery Build()
        {
            if (HttpClient == null)
            {
                if (string.IsNullOrEmpty(EurekaUrl))
                    throw new ArgumentNullException(nameof(EurekaUrl));

                HttpClient = new HttpClient(EurekaUrl);
            }

            return new EurekaAppCache(HttpClient);
        }
    }

    class EurekaAppCache : IEurekaDiscovery
    {

        //private final LoadingCache<String, Optional<App>> appCache;

        internal EurekaAppCache(IEurekaDiscovery eurekaClient)
        {
            //this.appCache = CacheBuilder.newBuilder()
            //        .expireAfterWrite(30, TimeUnit.SECONDS)
            //        .build(new CacheLoader<String, Optional<App>>() {
            //                    @Override
            //                    public Optional<App> load(String vip)
            //{
            //    return eurekaClient.lookup(vip);
            //}
            //});
        }

        public EurekaDiscovery.App Lookup(string vip)
        {
                return null;
            //return Optional.ofNullable(vip)
            //        .filter(v-> !Strings.isNullOrEmpty(v))
                    //.flatMap(appCache::getUnchecked);
        }

    }
}
