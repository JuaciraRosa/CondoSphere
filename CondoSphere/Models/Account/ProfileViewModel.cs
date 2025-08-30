namespace CondoSphere.Models.Account
{
    public class ProfileViewModel
    {
        public string Id { get; set; }          // read-only
        public string Email { get; set; }       // show; opcional editar
        public string FullName { get; set; }    // editable

       
        public string CompanyName { get; set; } // read-only (se houver)
        public string Role { get; set; }        // read-only

        public string? ProfileImagePath { get; set; }

    }
}
