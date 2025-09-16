namespace CondoSphere.API.Models.Dtos
{
    public class LoginDto
    {
        public string EmailOrUser { get; set; } = "";

        public string Password { get; set; } = "";
       public bool RememberMe { get; set; }


   
      
    }
}
