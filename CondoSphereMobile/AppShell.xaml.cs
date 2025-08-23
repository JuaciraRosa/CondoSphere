using CondoSphereMobile.Views;

namespace CondoSphereMobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(UsersPage), typeof(UsersPage));
            Routing.RegisterRoute(nameof(CompaniesPage), typeof(CompaniesPage));
            Routing.RegisterRoute(nameof(CondominiumsPage), typeof(CondominiumsPage));
            Routing.RegisterRoute(nameof(MeetingsPage), typeof(MeetingsPage));
            Routing.RegisterRoute(nameof(ExpensesPage), typeof(ExpensesPage));
            Routing.RegisterRoute(nameof(UnitsPage), typeof(UnitsPage));
            Routing.RegisterRoute(nameof(PaymentsPage), typeof(PaymentsPage));
            Routing.RegisterRoute(nameof(QuotasPage), typeof(QuotasPage));
            Routing.RegisterRoute(nameof(MaintenanceRequestsPage), typeof(MaintenanceRequestsPage));
            Routing.RegisterRoute(nameof(NotificationsPage), typeof(NotificationsPage));
        }
    }

}
