namespace CondoSphere.Models
{
    public class FinanceSnapshot
    {
        public decimal MonthExpenses { get; set; }
        public decimal MonthQuotasIn { get; set; }
        public decimal MonthBalance => MonthQuotasIn - MonthExpenses;
        public int OpenPayments { get; set; }
    }
}
