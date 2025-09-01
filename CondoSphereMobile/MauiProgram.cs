using CondoSphereMobile.Services;
using CondoSphereMobile.Views;
using Microsoft.Extensions.Logging;

namespace CondoSphereMobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>();

            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<SessionService>();

            // ViewModels/Pages que fores usar
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<UnitsPage>();
            builder.Services.AddTransient<QuotasPage>();
            builder.Services.AddTransient<MeetingsPage>();
            builder.Services.AddTransient<OccurrencesPage>();
            builder.Services.AddTransient<UsersPage>();
            builder.Services.AddTransient<CompaniesPage>();
            builder.Services.AddTransient<CondominiumsPage>();
            builder.Services.AddTransient<ExpensesPage>();
            builder.Services.AddTransient<PaymentsPage>();
            builder.Services.AddTransient<NotificationsPage>();
            builder.Services.AddTransient<VotingPage>();

            return builder.Build();
        }
    }
}

