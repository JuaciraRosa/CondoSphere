namespace CondoSphere.ViewModels
{
    public class AccountStatementEntry
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
        public decimal? Debit { get; set; }   // Quota
        public decimal? Credit { get; set; }  // Approved payment
        public decimal BalanceAfter { get; set; }
        public string? Status { get; set; }   // e.g., "Quota", "Payment (Succeeded)", "Payment (Pending)"
    }
}
