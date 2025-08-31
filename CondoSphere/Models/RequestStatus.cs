using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public enum RequestStatus
    {
        [Display(Name = "Open")]
        Open,

        [Display(Name = "In Progress")]
        InProgress,

        [Display(Name = "Resolved")]
        Resolved,

        [Display(Name = "Rejected")]
        Rejected
    }

}
