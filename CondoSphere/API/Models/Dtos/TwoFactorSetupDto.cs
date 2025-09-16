namespace CondoSphere.API.Models.Dtos
{
    public class TwoFactorSetupDto
    {

        public bool AlreadyEnabled { get; set; }
        public string SecretKey { get; set; } = "";   
        public string OtpAuthUri { get; set; } = ""; 
        public string QrPngBase64 { get; set; } = ""; 
    }
}
