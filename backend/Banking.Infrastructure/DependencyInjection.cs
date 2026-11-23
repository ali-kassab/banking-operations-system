using Banking.Application.Abstractions;
using Banking.Infrastructure.Data;
using Banking.Infrastructure.Repositories;
using Banking.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Banking.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Wires the Application-layer interfaces to their EF Core / security implementations.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, JwtSettings jwtSettings)
    {
        services.AddDbContext<BankingDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IBankAccountRepository, BankAccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        services.AddSingleton(jwtSettings);
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
