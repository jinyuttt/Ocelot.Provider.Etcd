namespace Ocelot.Provider.Etcd
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using dotnet_etcd;
    using Infrastructure.Extensions;
    using Logging;
    using Newtonsoft.Json;
    using ServiceDiscovery.Providers;
    using Values;
    using System.Linq;
    using Mvccpb;
    public class Etcd : IServiceDiscoveryProvider
    {
        private readonly EtcdRegistryConfiguration _config=null;
        private readonly IOcelotLogger _logger=null;
        //  private readonly EtcdClient _etcdClient=null;
        private IEtcdClientFactory _etcdClientFactory = null;
        private const string VersionPrefix = "version-";

        private List<Service> Services { get; set; }

        public Etcd(EtcdRegistryConfiguration config, IOcelotLoggerFactory factory, IEtcdClientFactory clientFactory)
        {
            _logger = factory.CreateLogger<Etcd>();
            _config = config;
            // _etcdClient = clientFactory.Get(_config);
            _etcdClientFactory = clientFactory;
            Services = new List<Service>();
        }

        /// <summary>
        /// ocelot每次请求都会调用
        /// </summary>
        /// <returns></returns>
        public async Task<List<Service>> Get()
        {
            // /Ocelot/Services/srvname/srvid
            if (Services.Count == 0)
            {
                EtcdClient client = _etcdClientFactory.Get(_config);
                var queryResult = await client.GetRangeAsync($"/Ocelot/Services/{_config.KeyOfServiceInEtcd}");
                foreach (var dic in queryResult.Kvs)
                {
                    var srvs = Util.FromGoogleString(dic.Key).Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (srvs.Length == 4)
                    {
                        var serviceEntry = JsonConvert.DeserializeObject<ServiceEntry>(Util.FromGoogleString(dic.Value));
                        serviceEntry.Name = srvs[2];
                        serviceEntry.Id = srvs[3];
                        if (IsValid(serviceEntry))
                        {
                            Services.Add(BuildService(serviceEntry));
                        }
                        else
                        {
                            _logger.LogWarning($"Unable to use service Address: {serviceEntry.Host} and Port: {serviceEntry.Port} as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0");
                        }
                    }
                }
            }
            return new List<Service>(Services);
        }

        private Service BuildService(ServiceEntry serviceEntry)
        {
            return new Service(
                serviceEntry.Name,
                new ServiceHostAndPort(serviceEntry.Host, serviceEntry.Port),
                serviceEntry.Id,
              string.IsNullOrWhiteSpace(serviceEntry.Version) ? GetVersionFromStrings(serviceEntry.Tags) : serviceEntry.Version,
                serviceEntry.Tags ?? Enumerable.Empty<string>());
        }

        private bool IsValid(ServiceEntry serviceEntry)
        {
            if (string.IsNullOrEmpty(serviceEntry.Host) || serviceEntry.Host.Contains("http://") || serviceEntry.Host.Contains("https://") || serviceEntry.Port <= 0)
            {
                return false;
            }
            return true;
        }

        private string GetVersionFromStrings(IEnumerable<string> strings)
        {
            return strings
                ?.FirstOrDefault(x => x.StartsWith(VersionPrefix, StringComparison.Ordinal))
                .TrimStart(VersionPrefix);
        }

        private void MonitorKeys()
        {
           var client=this._etcdClientFactory.Get(_config);
            client.WatchRange($"/Ocelot/Services/{_config.KeyOfServiceInEtcd}", new Action<WatchEvent[]>(p =>
             {
                 foreach (var w in p)
                 {
                     switch (w.Type)
                     {
                         case Event.Types.EventType.Put:
                             Add(w.Key, w.Value);
                             break;
                         case Event.Types.EventType.Delete:
                             Delete(w.Key, w.Value);
                             break;
                     }
                 }
             }));
        }
        private void Add(string key,string value)
        {
            var srvs = key.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var serviceEntry = JsonConvert.DeserializeObject<ServiceEntry>(value);
            serviceEntry.Name = srvs[2];
            serviceEntry.Id = srvs[3];
            if (IsValid(serviceEntry))
            {
                Services.Add(BuildService(serviceEntry));
            }
            else
            {
                _logger.LogWarning($"Unable to use service Address: {serviceEntry.Host} and Port: {serviceEntry.Port} as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0");
            }
        }

        private void Delete(string key, string value)
        {
            var srvs = key.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var serviceEntry = JsonConvert.DeserializeObject<ServiceEntry>(value);
            serviceEntry.Name = srvs[2];
            serviceEntry.Id = srvs[3];
            if (IsValid(serviceEntry))
            {
                Services.Remove(Services.Find(x => x.Id == serviceEntry.Id));
            }
            else
            {
                _logger.LogWarning($"Unable to use service Address: {serviceEntry.Host} and Port: {serviceEntry.Port} as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0");
            }
        }
    }
}