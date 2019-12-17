namespace Ocelot.Provider.Etcd.Cluster
{
    using dotnet_etcd;

    public interface IEtcdClientFactory
    {
        EtcdClient Get(EtcdRegistryConfiguration config);
    }
}