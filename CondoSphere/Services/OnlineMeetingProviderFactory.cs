namespace CondoSphere.Services
{
    public class OnlineMeetingProviderFactory : IOnlineMeetingProviderFactory
    {
        private readonly IServiceProvider _sp;
        private readonly IConfiguration _cfg;

        public OnlineMeetingProviderFactory(IServiceProvider sp, IConfiguration cfg)
        {
            _sp = sp; _cfg = cfg;
        }

        public IOnlineMeetingProvider Get(string providerName)
        {
            var name = providerName?.Trim().ToLowerInvariant()
                       ?? _cfg["OnlineMeetings:DefaultProvider"]?.ToLowerInvariant()
                       ?? "zoom";

            return name switch
            {
                "zoom" => _sp.GetRequiredService<ZoomOnlineMeetingProvider>(),
                "google" => _sp.GetRequiredService<GoogleMeetOnlineMeetingProvider>(),
                _ => _sp.GetRequiredService<ZoomOnlineMeetingProvider>()
            };
        }
    }
}