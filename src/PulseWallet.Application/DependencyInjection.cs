using Microsoft.Extensions.DependencyInjection;
using PulseWallet.Application.Transfers.CreateTransfer;
using PulseWallet.Application.Transfers.GetTransferById;
using PulseWallet.Application.Wallets.CreateWallet;
using PulseWallet.Application.Wallets.GetWalletById;
using PulseWallet.Application.Wallets.GetWalletLedgerEntries;
using PulseWallet.Application.Wallets.TopUpWallet;
using PulseWallet.Application.Wallets.WithdrawFromWallet;

namespace PulseWallet.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateWalletHandler>();
        services.AddScoped<GetWalletByIdHandler>();
        services.AddScoped<GetWalletLedgerEntriesHandler>();
        services.AddScoped<TopUpWalletHandler>();
        services.AddScoped<WithdrawFromWalletHandler>();
        services.AddScoped<CreateTransferHandler>();
        services.AddScoped<GetTransferByIdHandler>();

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}