using CommunityToolkit.Maui;
using CondoSphereMobile.Services;
using CondoSphereMobile.Views;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace CondoSphereMobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddSingleton<ApiService>();
            // páginas usadas neste fluxo
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<MaintenanceMyPage>();
            builder.Services.AddTransient<OccurrencesPage>();

             builder.Services.AddTransient<CondominiumsPage>();
            builder.Services.AddTransient<QuotasPage>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<ChatListPage>();
            builder.Services.AddTransient<ChatThreadPage>();
            builder.Services.AddTransient<AccountPage>();
            builder.Services.AddTransient<PaymentPage>();


            // força cultura pt-PT no app inteiro (formata {0:C} como "5,00 €")
            var euro = new CultureInfo("pt-PT");
            CultureInfo.DefaultThreadCurrentCulture = euro;
            CultureInfo.DefaultThreadCurrentUICulture = euro;


            return builder.Build();
        }
    }
}
