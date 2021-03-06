﻿namespace Ocelot.Provider.Etcd
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using ServiceDiscovery.Providers;
    using Values;

    public class PollEtcd : IServiceDiscoveryProvider
    {
        private readonly IOcelotLogger _logger;
        private readonly IServiceDiscoveryProvider _etcdServiceDiscoveryProvider;
        private readonly Timer _timer;
        private bool _polling;
        private List<Service> _services;

        public PollEtcd(int pollingInterval, IOcelotLoggerFactory factory, IServiceDiscoveryProvider etcdServiceDiscoveryProvider)
        {
            _logger = factory.CreateLogger<PollEtcd>();
            _etcdServiceDiscoveryProvider = etcdServiceDiscoveryProvider;
            _services = new List<Service>();

            _timer = new Timer(
                async x =>
            {
                if (_polling)
                {
                    return;
                }

                _polling = true;
                await Poll();
                _polling = false;
            }, null, pollingInterval, pollingInterval);
        }

        public Task<List<Service>> Get()
        {
            return Task.FromResult(_services);
        }

        private async Task Poll()
        {
            _services = await _etcdServiceDiscoveryProvider.Get();
        }
    }
}