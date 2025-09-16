namespace CondoSphere.API.Models.Dtos
{
    public class ProfileDto
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string? FullName { get; set; }
        public string? ProfileImageUrl { get; set; }
        public bool TwoFactorEnabled { get; set; }

    }
}
