using CondoSphere.Data.Interfaces;
using CondoSphere.Data.Repositories;
using CondoSphere.Services;

namespace CondoSphere.Data.DependencyInjection
{
    public static class RepositoryService
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<UserService>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ICompanyRepository, CompanyRepository>();
            services.AddScoped<ICondominiumRepository, CondominiumRepository>();
            services.AddScoped<IUnitRepository, UnitRepository>();
            services.AddScoped<IExpenseRepository, ExpenseRepository>();
            services.AddScoped<IQuotaRepository, QuotaRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IMaintenanceRequestRepository, MaintenanceRequestRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IMeetingRepository, MeetingRepository>();

            return services;
        }
    }
}
