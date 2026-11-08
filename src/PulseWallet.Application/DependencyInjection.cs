using Microsoft.Extensions.DependencyInjection;
using PulseWallet.Application.Wallets.CreateWallet;

namespace PulseWallet.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the application layer's use case handlers.
    /// Each layer registers its own services, so Program.cs doesn't need to know every handler.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateWalletHandler>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}