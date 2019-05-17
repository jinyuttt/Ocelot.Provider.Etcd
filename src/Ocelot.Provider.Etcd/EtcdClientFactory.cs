namespace Ocelot.Provider.Etcd
{
    using dotnet_etcd;
    using etcd.Provider.Cluster.Extensions;

    public class EtcdClientFactory : IEtcdClientFactory
    {
        private string host = "";
        private int port = -1;
        private EtcdClient client = null;
        public EtcdClient Get(EtcdRegistryConfiguration config)
        {
            // return new EtcdClient(config.Host, config.Port);
            if (config.Host != host || config.Port != port)
            {
                host = config.Host;
                port = config.Port;
                client = new EtcdClient(config.Host, config.Port);
            }
            return client.GetEtcdClient().GetClient();
        }
    }
}