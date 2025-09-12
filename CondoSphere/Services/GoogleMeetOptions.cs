namespace CondoSphere.Services
{
    public class GoogleMeetOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string CalendarId { get; set; } = "primary";
        public string TimeZone { get; set; } = "UTC"; // ex.: "Europe/Lisbon"
        public string? OrganizerEmail { get; set; }   // opcional (Workspace)
        public int DefaultDurationMinutes { get; set; } = 60;
    }
}
