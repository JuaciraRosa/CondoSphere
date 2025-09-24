using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using CondoSphere.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace CondoSphere.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ICompanyRepository _companyRepo;
        private readonly ICondominiumRepository _condoRepo;
        private readonly IUserRepository _userRepo;

        private readonly IPaymentRepository _paymentRepo;
        private readonly IExpenseRepository _expenseRepo;
        private readonly IMeetingRepository _meetingRepo;
        private readonly IAnnouncementRepository _annRepo;
        private readonly IMaintenanceRequestRepository _maintRepo;

        public DashboardController(
            ICompanyRepository companyRepo,
            ICondominiumRepository condoRepo,
            IUserRepository userRepo,
            IPaymentRepository paymentRepo,
            IExpenseRepository expenseRepo,
            IMeetingRepository meetingRepo,
            IAnnouncementRepository annRepo,
            IMaintenanceRequestRepository maintRepo)
        {
            _companyRepo = companyRepo;
            _condoRepo = condoRepo;
            _userRepo = userRepo;

            _paymentRepo = paymentRepo;
            _expenseRepo = expenseRepo;
            _meetingRepo = meetingRepo;
            _annRepo = annRepo;
            _maintRepo = maintRepo;
        }

        public async Task<IActionResult> Index()
        {
            
            var companies = await _companyRepo.GetAllAsync();
            var condos = (await _condoRepo.GetAllAsync()).ToList();
            var users = await _userRepo.GetAllAsync();

            var role = User?.FindFirst("role")?.Value
                       ?? User?.FindFirst(ClaimTypes.Role)?.Value;

            var model = new DashboardViewModel
            {
                TotalCompanies = companies.Count(),
                TotalCondominiums = condos.Count,
                TotalUsers = users.Count(),
                Role = role,
                CondoName = condos.FirstOrDefault()?.Name
            };

            // ===  original 3 KPI cards ===
            model.Kpis.AddRange(new[]
            {
                new Kpi { Title="Companies",    Icon="bi-building",  Value=model.TotalCompanies.ToString(),    Variant="primary" },
                new Kpi { Title="Condominiums", Icon="bi-buildings", Value=model.TotalCondominiums.ToString(), Variant="info"    },
                new Kpi { Title="Users",        Icon="bi-people",    Value=model.TotalUsers.ToString(),        Variant="success" },
            });

            // === Admin-only extras ===
            if (string.Equals(role, "Administrator", StringComparison.OrdinalIgnoreCase))
            {
                // Finance (current month, UTC)
                var now = DateTime.UtcNow;
                var first = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var next = first.AddMonths(1);

                var income = await _paymentRepo.GetPaidTotalAsync(first, next);
                var expenses = await _expenseRepo.GetTotalAsync(first, next);
                var openPayments = await _paymentRepo.CountOpenAsync();
                var last6 = await _paymentRepo.CountPaidByMonthAsync(6);

                model.Finance = new FinanceSnapshot
                {
                    MonthQuotasIn = income,
                    MonthExpenses = expenses,
                    OpenPayments = openPayments
                };
                model.Last6MonthsPayments = last6;

                // Meetings — use first condominium (adjust if you track an "active condo")
                var firstCondoId = condos.FirstOrDefault()?.Id ?? 0;
                if (firstCondoId != 0)
                {
                    // repo signature in your project: GetUpcomingAsync(int condominiumId)
                    var meetings = await _meetingRepo.GetUpcomingAsync(firstCondoId);

                    model.UpcomingMeetings = meetings
                        .OrderBy(m => m.ScheduledDate)
                        .Take(5)
                        .Select(m => new SimpleItem
                        {
                            Id = m.Id,
                            Title = string.IsNullOrWhiteSpace(m.Agenda) ? "Meeting" : m.Agenda,
                            // no "Location" field in your model; show where/how:
                            Subtitle = m.IsOnline
                                        ? $"{(string.IsNullOrWhiteSpace(m.OnlineProvider) ? "Online" : m.OnlineProvider)} (online)"
                                        : (m.Condominium != null ? m.Condominium.Name : "On-site"),
                            When = m.ScheduledDate,
                            Url = Url.Action("Details", "Meetings", new { id = m.Id }),
                            Badge = "Upcoming",
                            BadgeVariant = "primary"
                        })
                        .ToList();
                }

                // Announcements — global (or filtered by the first condo if your repo supports it)
                int? condoIdForAnnouncements = condos.FirstOrDefault()?.Id;
                var anns = await _annRepo.GetLatestAsync(condoIdForAnnouncements, take: 5);
                model.LatestAnnouncements = anns.Select(a => new SimpleItem
                {
                    Id = a.Id,
                    Title = a.Title,
                    Subtitle = a.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    Url = Url.Action("Details", "Announcements", new { id = a.Id }),
                    Badge = "New",
                    BadgeVariant = "success"
                }).ToList();

                // Maintenance / incidents — open only
                var tickets = await _maintRepo.GetOpenRequestsAsync();
                model.OpenTickets = tickets
                    .OrderByDescending(t => t.SubmittedAt)
                    .Take(5)
                    .Select(t => new SimpleItem
                    {
                        Id = t.Id,
                        Title = t.Title,
                        // your entity has no Unit and no Progress; show condo name instead
                        Subtitle = t.Condominium != null ? t.Condominium.Name : null,
                        Url = Url.Action("Details", "MaintenanceRequests", new { id = t.Id }),
                        Badge = t.Status.ToString(),
                        BadgeVariant = t.Status == RequestStatus.InProgress ? "warning" :
                                       t.Status == RequestStatus.Open ? "secondary" : "success"
                        // Progress = null
                    })
                    .ToList();

                // Extra KPIs (real)
                var openTicketsCount = await _maintRepo.CountOpenAsync();
                model.Kpis.AddRange(new[]
                {
                    new Kpi { Title="Open Tickets",       Icon="bi-tools",          Value=openTicketsCount.ToString(),               Variant="warning",  Subtext="maintenance/incidents" },
                    new Kpi { Title="New Announcements",  Icon="bi-megaphone",      Value=model.LatestAnnouncements.Count.ToString(), Variant="secondary" },
                    new Kpi { Title="Upcoming Meetings",  Icon="bi-calendar-event", Value=model.UpcomingMeetings.Count.ToString(),   Variant="primary"   }
                });
            }

            return View(model);
        }
    }
}
