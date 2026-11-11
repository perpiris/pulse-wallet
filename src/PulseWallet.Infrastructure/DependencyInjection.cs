using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PulseWallet.Application.Abstractions;
using PulseWallet.Application.Wallets;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Wallets;
using PulseWallet.Infrastructure.Persistence;
using PulseWallet.Infrastructure.Persistence.Queries;
using PulseWallet.Infrastructure.Persistence.Repositories;

namespace PulseWallet.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the infrastructure layer: database access (and later repositories, external clients...).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Comes from the ConnectionStrings__PulseWallet environment variable in Docker (compose.yaml),
        // or from user secrets when running locally. Never from appsettings, so no password is committed.
        string connectionString = configuration.GetConnectionString("PulseWallet")
            ?? throw new InvalidOperationException("Connection string 'PulseWallet' is not configured.");

        // Scoped by default: one DbContext per HTTP request.
        services.AddDbContext<PulseWalletDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<ILedgerEntryRepository, LedgerEntryRepository>();
        services.AddScoped<IWalletQueries, WalletQueries>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PulseWalletDbContext>());

        return services;
    }
}