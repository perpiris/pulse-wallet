using Microsoft.Extensions.DependencyInjection;
using PulseWallet.Application.Wallets.CreateWallet;
using PulseWallet.Application.Wallets.GetWalletById;
using PulseWallet.Application.Wallets.TopUpWallet;
using PulseWallet.Application.Wallets.WithdrawFromWallet;

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
        services.AddScoped<GetWalletByIdHandler>();
        services.AddScoped<TopUpWalletHandler>();
        services.AddScoped<WithdrawFromWalletHandler>();

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}