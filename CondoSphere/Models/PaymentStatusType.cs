using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public enum PaymentStatusType
    {
        Pending,

        [Display(Name = "Requires Action")]
        RequiresAction,
        Succeeded,
        Failed,
        Canceled
    }
}
