﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;


namespace dotnet_eureka
{
    internal interface IEurekaDiscovery
    {
        EurekaDiscovery.App Lookup(string vip);
    }

    static class EurekaDiscovery
    {
        internal interface ILogger
        {
            void Warn(string format, params object[] args);
            void Debug(string format, params object[] args);
        }

        internal class App
        {
            static readonly Random random = new Random();
            readonly List<Instance> instances;

            internal App(List<Instance> vips)
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

            void Shuffle<T>(IList<T> list)
            {
                for(int n = list.Count; n > 1;) {
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
                internal Status Status { get; }
                internal string UrlString { get; }

                internal Instance(string urlString, string vipAddress, string status)
                {
                    Status = (Status)Enum.Parse(typeof(Status), status);
                    UrlString = urlString + vipAddress + "/";
                }
            }
        }

        internal class Builder
        {
            string EurekaUrl;
            IEurekaDiscovery EurekaClient;
            ILogger Logger;
            TimeSpan? CacheExpiration;

            public Builder SetEurekaUrl(string eurekaUrl)
            {
                EurekaUrl = eurekaUrl;
                return this;
            }

            public Builder SetEurekaClient(IEurekaDiscovery eurekaDiscovery)
            {
                EurekaClient = eurekaDiscovery;
                return this;
            }

            public Builder SetLogger(ILogger logger)
            {
                Logger = logger;
                return this;
            }

            public Builder SetCacheExiration(TimeSpan timeSpan)
            {
                CacheExpiration = timeSpan;
                return this;
            }

            public IEurekaDiscovery Build()
            {
                if (EurekaClient == null)
                {
                    if (string.IsNullOrEmpty(EurekaUrl))
                        throw new ArgumentNullException(nameof(EurekaUrl));

                    EurekaClient = new EurekaRestClient(EurekaUrl, Logger);
                }

                return new EurekaAppCache(EurekaClient, Logger, CacheExpiration);
            }
        }
    }

    class NoopLogger : EurekaDiscovery.ILogger
    {
        public void Warn(string format, params object[] args) { }
        public void Debug(string format, params object[] args) { }
    }

    class EurekaRestClient : IEurekaDiscovery
    {
        readonly string QUERY_APP_PATH = "/eureka/apps/{vip}";
        readonly EurekaDiscovery.ILogger Log;
        readonly string eurekaHost;
        readonly int eurekaPort;

        internal EurekaRestClient(String host, EurekaDiscovery.ILogger logger)
        {
            if (string.IsNullOrEmpty(host))
                throw new ArgumentNullException(nameof(host));

            var parts = host.Split(":");
            if(parts.Length > 1)
            {
                eurekaHost = parts[0];
                eurekaPort = int.Parse(parts[1]);
            }
            else
            {
                eurekaHost = host;
                eurekaPort = 80;
            }

            Log = logger ?? new NoopLogger();
        }

        public EurekaDiscovery.App Lookup(string vip)
        {
            string path = QUERY_APP_PATH.Replace("{vip}", vip);
            HttpResponse response;
            try
            {
                response = HttpRequest(eurekaHost, eurekaPort, path);
            } catch(Exception e)
            {
                Log.Warn("lookup {0} - Failed against eureka server {1}. Cause={2}", vip, eurekaHost, e);
                return null;
            }

            if(response.Status != 200)
            {
                Log.Warn("lookup {0} - Failed because Eureka server {1} returned HTTP {2}", vip, eurekaHost, response.Status);
                return null;
            }

            return ParseJsonResponseFrom(response.Content);
        }

        EurekaDiscovery.App ParseJsonResponseFrom(byte[] data)
        {
            string json = Encoding.UTF8.GetString(data);
            int instancesStart = json.IndexOf("[{", StringComparison.Ordinal);
            int instancesEnd = json.IndexOf("}]", StringComparison.Ordinal);
            if (instancesStart < 0 || instancesEnd < 0)
                return new EurekaDiscovery.App(new List<EurekaDiscovery.App.Instance>());

            var instanceSlices = json.Substring(instancesStart, instancesEnd - instancesStart).Split("},{");

            var instances = new List<EurekaDiscovery.App.Instance>();
            foreach (var i in instanceSlices)
                instances.Add(ReadAppInstance(i));

            return new EurekaDiscovery.App(instances);
        }

        EurekaDiscovery.App.Instance ReadAppInstance(string instance)
        {
            return new EurekaDiscovery.App.Instance(
                    ReadStringProperty(instance, "homePageUrl"),
                    ReadStringProperty(instance, "vipAddress"),
                    ReadStringProperty(instance, "status"));
        }

        string ReadStringProperty(string json, string name)
        {
            int idx = json.IndexOf(name, StringComparison.Ordinal);
            if (idx < 0)
                return null;

            idx += name.Length + 1;
            while (char.IsWhiteSpace(json[idx])) { idx++; }

            if (json[idx] != ':')
                throw new Exception("Expected ':' while looking for property " + name);

            idx++;
            while (char.IsWhiteSpace(json[idx])) { idx++; }

            if(json[idx] != '\"')
                throw new Exception("Expected '\"' while reading the value of property " + name);

            idx++;
            int valueStart = idx;
            while (json[idx++] != '\"') { }
            int valueEnd = idx;

            return json.Substring(valueStart, valueEnd - valueStart - 1);
        }

        class HttpResponse
        {
            internal int Status { get; set; }
            internal byte[] Content { get; set; }
        }

        HttpResponse HttpRequest(string host, int port, string path)
        {
            HttpResponse response = new HttpResponse();
            byte[] rawResponse;
            using (var tcp = new TcpClient(host, port))
            using (var stream = tcp.GetStream())
            {
                tcp.SendTimeout = 500;
                tcp.ReceiveTimeout = 1000;

                var requestHeaders = SetupRequestHeaders(host, path);
                stream.Write(requestHeaders, 0, requestHeaders.Length);

                rawResponse = ReceiveResponse(stream);
            }

            var index = BinaryMatch(rawResponse, 0, Encoding.ASCII.GetBytes("\r\n\r\n")); 
            var headers = ParseHeaders(Encoding.ASCII.GetString(rawResponse, 0, index));

            response.Status = (int)headers["responseCode"];
            response.Content = ReadContent(rawResponse, index + 4, headers);

            return response;
        }

        byte[] SetupRequestHeaders(string host, string path)
        {
            var builder = new StringBuilder();
            builder.AppendLine("GET " + path + " HTTP/1.1");
            builder.AppendLine("Host: " + host);
            builder.AppendLine("Accept: application/json");
            builder.AppendLine("Connection: close");
            builder.AppendLine();
            return Encoding.ASCII.GetBytes(builder.ToString());
        }

        byte[] ReceiveResponse(NetworkStream stream)
        {
            using (var memory = new MemoryStream())
            {
                stream.CopyTo(memory);
                memory.Position = 0;
                return memory.ToArray();
            }
        }

        Dictionary<string, object> ParseHeaders(string headers)
        {
            var result = new Dictionary<string, object>();
            var headersArray = headers.Split("\r\n");
            foreach (string header in headersArray)
            {
                if(header.StartsWith("HTTP/1.1", StringComparison.Ordinal))
                {
                    result.Add("responseCode", int.Parse(header.Split(" ")[1]));
                }
                else
                {
                    int separator = header.IndexOf(":", StringComparison.Ordinal);
                    result.Add(header.Substring(0, separator), header.Substring(separator + 2));
                }
            }
            return result;
        }

        byte[] ReadContent(byte[] rawResponse, int index, Dictionary<string, object> headers)
        {
            if (headers.ContainsKey("Transfer-Encoding") && "chunked".Equals(headers["Transfer-Encoding"]))
            {
                return ReadChunked(rawResponse, index);
            }

            // plain content, no encoding, no compression etc
            var content = new byte[rawResponse.Length - index];
            rawResponse.CopyTo(content, index);
            return content;
        }

        byte[] ReadChunked(byte[] data, int offset)
        {
            using (var buffer = new MemoryStream())
            {
                byte[] EOL = Encoding.ASCII.GetBytes("\r\n");
                int chunkStart = offset;
                int chunkSize;

                while (true)
                {
                    int idx = BinaryMatch(data, chunkStart, EOL);
                    chunkSize = idx > 0 ? Convert.ToInt32(Encoding.ASCII.GetString(data, chunkStart, idx - chunkStart), 16) : 0;

                    if (chunkSize == 0)
                        break;

                    buffer.Write(data, idx + EOL.Length, chunkSize);

                    chunkStart = idx + EOL.Length + chunkSize + EOL.Length;
                }

                buffer.Position = 0;
                return buffer.ToArray();
            }
        }

        private int BinaryMatch(byte[] input, int offset, byte[] pattern)
        {
            int sLen = input.Length + offset - pattern.Length + 1;
            for (int i = offset; i < sLen; ++i)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; ++j)
                {
                    if (input[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    return i;
                }
            }
            return -1;
        }
    }

    class EurekaAppCache : IEurekaDiscovery
    {
        private static readonly TimeSpan DEFAULT_CACHE_EXPIRATION = TimeSpan.FromSeconds(30);
        private static readonly EurekaDiscovery.App EMPTY_CLUSTER = new EurekaDiscovery.App(new List<EurekaDiscovery.App.Instance>());

        readonly TimeSpan CacheExpiration = TimeSpan.FromSeconds(30);
        readonly EurekaDiscovery.ILogger Log;
        readonly IEurekaDiscovery Discovery;
        readonly Dictionary<string, CachedApp> Cache;

        internal EurekaAppCache(IEurekaDiscovery eurekaClient, EurekaDiscovery.ILogger logger, TimeSpan? cacheExpiration)
        {
            Discovery = eurekaClient;
            Log = logger ?? new NoopLogger();
            Cache = new Dictionary<string, CachedApp>();
            CacheExpiration = cacheExpiration.GetValueOrDefault(DEFAULT_CACHE_EXPIRATION);
        }

        public EurekaDiscovery.App Lookup(string vip)
        {
            if (string.IsNullOrEmpty(vip))
                return null;

            DateTime now = DateTime.Now;
            CachedApp value = ComputeIfAbsent(vip);
            if (value.Expiration.CompareTo(now) < 0)
                lock(value)
                {
                    if (value.Expiration.CompareTo(now) >= 0)
                        return value.App; // refreshed on a separate thread

                    Log.Debug("EurekaAppCache - Loading vip {0}", vip);
                    value.App = Discovery.Lookup(vip);
                    value.Expiration = now.Add(CacheExpiration);
                }

            return value.App;
        }

        CachedApp ComputeIfAbsent(string vip)
        {
            lock(Cache)
            {
                Cache.TryGetValue(vip, out CachedApp value);
                if (value != null)
                    return value; // got created on another thread

                Log.Debug("EurekaAppCache - Registering vip {0}", vip);
                value = new CachedApp { Expiration = DateTime.MinValue, App = EMPTY_CLUSTER };
                Cache.Add(vip, value);
                return value;
            }
        }

        class CachedApp
        {
            // these need to be fields to get synchronization on get/set
            internal DateTime Expiration;
            internal EurekaDiscovery.App App;
        }
    }
}
