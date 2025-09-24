namespace CondoSphere.API.Models
{
    public class CreateMaintenanceRequestDto
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int CondominiumId { get; set; }
        // NADA de SubmittedById aqui
    }
}
