namespace CondoSphere.API.Models.Dtos
{
    public class BootstrapDto
    {
        public bool Enabled { get; set; }
        public string Key { get; set; } = "";
        public string KeyFormatted { get; set; } = "";
        public string OtpAuthUri { get; set; } = "";
        public string QrPngDataUrl { get; set; } = "";
    }
}
