namespace CondoSphere.Dtos
{
    public class PaymentsSummaryDto
    {
        public int Month { get; set; }
        public string MonthName { get; set; } = "";
        public decimal Total { get; set; }
        public int Count { get; set; }
    }
}
