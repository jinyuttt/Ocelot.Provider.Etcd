namespace Ocelot.Provider.Etcd.Cluster
{
    using dotnet_etcd;
    using etcd.Provider.Cluster.Extensions;

    public class EtcdClientFactory : IEtcdClientFactory
    {
        private string host=null;
        private int port = -1;
        private EtcdClient client = null;

        public EtcdClient Get(EtcdRegistryConfiguration config)
        {
           
            if (config.Host != this.host || config.Port != this.port)
            {
                this.host = config.Host;
                this.port = config.Port;
                this.client = new EtcdClient(config.Host, config.Port);
            }

            return client.GetEtcdClient().GetClient();
        }
    }
}