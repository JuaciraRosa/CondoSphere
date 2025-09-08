namespace CondoSphere.Dtos
{
    public class QuotasByMonthDto
    {
        public int Month { get; set; }
        public string MonthName { get; set; } = "";
        public int Paid { get; set; }
        public int Late { get; set; }
    }
}
