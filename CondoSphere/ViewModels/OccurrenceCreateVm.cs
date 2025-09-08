using Microsoft.AspNetCore.Mvc.Rendering;

namespace CondoSphere.ViewModels
{
    public class OccurrenceCreateVm
    {
        public int CondominiumId { get; set; }
        public int UnitId { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Email { get; set; } = "";

        // dropdowns
        public IEnumerable<SelectListItem> Condominiums { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Units { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
