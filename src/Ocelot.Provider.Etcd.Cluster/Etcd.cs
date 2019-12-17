namespace Ocelot.Provider.Etcd.Cluster
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using dotnet_etcd;
    using Mvccpb;
    using Newtonsoft.Json;
    using Ocelot.Infrastructure.Extensions;
    using Ocelot.Logging;
    using Ocelot.ServiceDiscovery.Providers;
    using Ocelot.Values;

    public class Etcd : IServiceDiscoveryProvider
    {
        private readonly EtcdRegistryConfiguration config=null;
        private readonly IOcelotLogger logger=null;
        private readonly IEtcdClientFactory etcdClientFactory = null;
        private const string VersionPrefix = "version-";

        private List<Service> Services { get; set; }

        public Etcd(EtcdRegistryConfiguration config, IOcelotLoggerFactory factory, IEtcdClientFactory clientFactory)
        {
            logger = factory.CreateLogger<Etcd>();
            this.config = config;
            this.etcdClientFactory = clientFactory;
            Services = new List<Service>();
        }

        /// <summary>
        /// ocelot每次请求都会调用
        /// </summary>
        /// <returns></returns>
        public async Task<List<Service>> Get()
        {
            //Ocelot/Services/srvname/srvid
            if (Services.Count == 0)
            {
                EtcdClient client = etcdClientFactory.Get(this.config);
                MonitorKeys();
                var queryResult = await client.GetRangeAsync($"/Ocelot/Services/{config.KeyOfServiceInEtcd}");
                foreach (var dic in queryResult.Kvs)
                {
                    var srvs = Util.FromGoogleString(dic.Key).Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (srvs.Length == 4)
                    {
                        var serviceEntry = JsonConvert.DeserializeObject<ServiceEntry>(Util.FromGoogleString(dic.Value));
                        serviceEntry.Name = srvs[2];
                        serviceEntry.Id = srvs[3];
                        if (this.IsValid(serviceEntry))
                        {
                            this.Services.Add(BuildService(serviceEntry));
                        }
                        else
                        {
                            this.logger.LogWarning($"Unable to use service Address: {serviceEntry.Host} and Port: {serviceEntry.Port} as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0");
                        }
                    }
                }
            }
            return new List<Service>(Services);
        }

        /// <summary>
        /// 添加服务列表
        /// </summary>
        /// <param name="serviceEntry"></param>
        /// <returns></returns>
        private Service BuildService(ServiceEntry serviceEntry)
        {
            return new Service(
                serviceEntry.Name,
                new ServiceHostAndPort(serviceEntry.Host, serviceEntry.Port),
                serviceEntry.Id,
              string.IsNullOrWhiteSpace(serviceEntry.Version) ? this.GetVersionFromStrings(serviceEntry.Tags) : serviceEntry.Version,
                serviceEntry.Tags ?? Enumerable.Empty<string>());
        }

        /// <summary>
        /// 验证接口
        /// </summary>
        /// <param name="serviceEntry"></param>
        /// <returns></returns>
        private bool IsValid(ServiceEntry serviceEntry)
        {
            if (string.IsNullOrEmpty(serviceEntry.Host) || serviceEntry.Host.Contains("http://") || serviceEntry.Host.Contains("https://") || serviceEntry.Port <= 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 获取版本信息
        /// </summary>
        /// <param name="strings"></param>
        /// <returns></returns>
        private string GetVersionFromStrings(IEnumerable<string> strings)
        {
            return strings
                ?.FirstOrDefault(x => x.StartsWith(VersionPrefix, StringComparison.Ordinal))
                .TrimStart(VersionPrefix);
        }

        /// <summary>
        /// 监视服务信息
        /// </summary>
        private void MonitorKeys()
        {
            var client=this.etcdClientFactory.Get(this.config);
            client.WatchRange($"/Ocelot/Services/{this.config.KeyOfServiceInEtcd}", new Action<WatchEvent[]>(p =>
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

        /// <summary>
        /// 添加服务
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void Add(string key,string value)
        {
            var srvs = key.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var serviceEntry = JsonConvert.DeserializeObject<ServiceEntry>(value);
            serviceEntry.Name = srvs[2];
            serviceEntry.Id = srvs[3];
            if (this.IsValid(serviceEntry))
            {
                Services.Add(BuildService(serviceEntry));
            }
            else
            {
                logger.LogWarning($"Unable to use service Address: {serviceEntry.Host} and Port: {serviceEntry.Port} as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0");
            }
        }

        /// <summary>
        /// 删除服务
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void Delete(string key, string value)
        {
            var srvs = key.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var serviceEntry = JsonConvert.DeserializeObject<ServiceEntry>(value);
            serviceEntry.Name = srvs[2];
            serviceEntry.Id = srvs[3];
            if (this.IsValid(serviceEntry))
            {
                this.Services.Remove(Services.Find(x => x.Id == serviceEntry.Id));
            }
            else
            {
                this.logger.LogWarning($"Unable to use service Address: {serviceEntry.Host} and Port: {serviceEntry.Port} as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0");
            }
        }
    }
}