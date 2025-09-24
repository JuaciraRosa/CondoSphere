namespace CondoSphere.API.Models
{
    public class CreateThreadReq
    {
        public string? Subject { get; set; }
        public int? CondominiumId { get; set; }
        public string? ResidentId { get; set; } 
    }
}
