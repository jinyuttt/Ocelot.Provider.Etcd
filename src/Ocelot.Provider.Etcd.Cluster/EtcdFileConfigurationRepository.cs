using System;
using System.Threading.Tasks;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using dotnet_etcd;
using Ocelot.Logging;
using Newtonsoft.Json;
using Ocelot.Responses;

namespace Ocelot.Provider.Etcd.Cluster
{
    public class EtcdFileConfigurationRepository : IFileConfigurationRepository
    {
        private readonly EtcdClient _etcdClient;
        private readonly string _configurationKey;
        private readonly Cache.IOcelotCache<FileConfiguration> _cache;
        private readonly IOcelotLogger _logger;

        public EtcdFileConfigurationRepository(
            Cache.IOcelotCache<FileConfiguration> cache,
            IInternalConfigurationRepository repo,
            IEtcdClientFactory factory,
            IOcelotLoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<EtcdFileConfigurationRepository>();
            _cache = cache;

            var internalConfig = repo.Get();

            _configurationKey = "InternalConfiguration";

            string token = null;

            if (!internalConfig.IsError)
            {
                token = internalConfig.Data.ServiceProviderConfiguration.Token;
                _configurationKey = !string.IsNullOrEmpty(internalConfig.Data.ServiceProviderConfiguration.ConfigurationKey) ?
                    internalConfig.Data.ServiceProviderConfiguration.ConfigurationKey : _configurationKey;
            }

            var config = new EtcdRegistryConfiguration(
                internalConfig.Data.ServiceProviderConfiguration.Host,
                internalConfig.Data.ServiceProviderConfiguration.Port, _configurationKey);

            _etcdClient = factory.Get(config);
        }

        public async Task<Response<FileConfiguration>> Get()
        {
            var config = _cache.Get(_configurationKey, _configurationKey);

            if (config != null)
            {
                return new OkResponse<FileConfiguration>(config);
            }
            var queryResult = await _etcdClient.GetValAsync($"/{_configurationKey}");
            if (string.IsNullOrWhiteSpace(queryResult))
            {
                return new OkResponse<FileConfiguration>(null);
            }
            var etcdConfig = JsonConvert.DeserializeObject<FileConfiguration>(queryResult);
            return new OkResponse<FileConfiguration>(etcdConfig);
        }

        public async Task<Response> Set(FileConfiguration ocelotConfiguration)
        {
            var json = JsonConvert.SerializeObject(ocelotConfiguration, Formatting.Indented);
            var result = await _etcdClient.PutAsync($"/{_configurationKey}", json);

            _cache.AddAndDelete(_configurationKey, ocelotConfiguration, TimeSpan.FromSeconds(3), _configurationKey);

            return new OkResponse();

            // return new ErrorResponse(new UnableToSetConfigInEtcdError($"Unable to set FileConfiguration in etcd, response status code from etcd was {result.StatusCode}"));
        }
    }
}