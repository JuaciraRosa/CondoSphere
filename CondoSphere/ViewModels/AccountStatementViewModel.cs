using CondoSphere.Models;

namespace CondoSphere.ViewModels
{

    public class AccountStatementViewModel
    {
        public int UnitId { get; set; }
        public string UnitLabel { get; set; } = "";
        public decimal OpeningBalance { get; set; }
        public List<AccountStatementLine> Lines { get; set; } = new();
        public decimal RunningBalance { get; set; }
    }
}
