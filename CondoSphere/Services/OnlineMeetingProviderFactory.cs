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
            var name = providerName?.Trim() ?? _cfg["OnlineMeetings:DefaultProvider"] ?? "Zoom";
            return name.ToLowerInvariant() switch
            {
                "zoom" => _sp.GetRequiredService<ZoomOnlineMeetingProvider>(),
                //"teams"  => _sp.GetRequiredService<TeamsOnlineMeetingProvider>(),
                //"google" => _sp.GetRequiredService<GoogleMeetOnlineMeetingProvider>(),
                _ => _sp.GetRequiredService<ZoomOnlineMeetingProvider>()
            };
        }
    }
}
