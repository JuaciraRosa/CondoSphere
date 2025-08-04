namespace CondoSphere.Models
{
    public class Quota
    {
        public int Id { get; set; }
        public int UnitId { get; set; }
        public Unit Unit { get; set; }

        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsPaid { get; set; }

        public Payment Payment { get; set; }
    }

}
