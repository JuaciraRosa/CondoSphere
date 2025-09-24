using CondoSphere.Models;

namespace CondoSphere.ViewModels
{
    public class DashboardViewModel
    {
        //  existing fields
        public int TotalCompanies { get; set; }
        public int TotalCondominiums { get; set; }
        public int TotalUsers { get; set; }

        // Extras
        public string? Role { get; set; }
        public string? CondoName { get; set; }

        public List<Kpi> Kpis { get; set; } = new();
        public FinanceSnapshot Finance { get; set; } = new();
        public int[] Last6MonthsPayments { get; set; } = Array.Empty<int>();

        public List<SimpleItem> UpcomingMeetings { get; set; } = new();
        public List<SimpleItem> LatestAnnouncements { get; set; } = new();
        public List<SimpleItem> OpenTickets { get; set; } = new();
    }
}
