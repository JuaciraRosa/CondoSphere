namespace CondoSphere.Models
{
    public class AccountStatementLine
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
        public decimal Debit { get; set; }     // a pagar
        public decimal Credit { get; set; }    // pagos/aprovados
        public string Status { get; set; } = "";
        public int? PaymentId { get; set; }
    }
}
