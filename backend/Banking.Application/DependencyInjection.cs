using Banking.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Banking.Application;

public static class DependencyInjection
{
    /// <summary>Registers the business services. They are plain classes, so no interfaces are needed.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<AccountService>();
        services.AddScoped<TransactionService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<UserService>();
        return services;
    }
}
