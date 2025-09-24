namespace CondoSphere.Services.Notifications
{
    public class TwilioSmsOptions
    {
        public string AccountSid { get; set; } = "";
        public string AuthToken { get; set; } = "";
        public string? FromNumber { get; set; }
        public string? MessagingServiceSid { get; set; }
        public string? Region { get; set; }
        public string? SubAccountSid { get; set; }
    }
}
