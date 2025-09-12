namespace CondoSphere.Services
{
    public interface IOnlineMeetingProviderFactory
    {
        IOnlineMeetingProvider Get(string providerName);
    }
}
