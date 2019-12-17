namespace Ocelot.Provider.Etcd.Cluster
{
    using Errors;

    public class UnableToSetConfigInEtcdError : Error
    {
        public UnableToSetConfigInEtcdError(string s)
            : base(s, OcelotErrorCode.UnknownError)
        {
        }
    }
}