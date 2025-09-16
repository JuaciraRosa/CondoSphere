namespace CondoSphere.API.Models.Dtos
{
    public class TokenDto
    {

        public string? Token { get; set; }
        public bool RequiresTwoFactor { get; set; }
    }
}
