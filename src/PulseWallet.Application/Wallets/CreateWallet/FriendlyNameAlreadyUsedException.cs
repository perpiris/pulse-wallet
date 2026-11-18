namespace PulseWallet.Application.Wallets.CreateWallet;

// Friendly names are unique per owner: you can't have two wallets called "Savings", but someone else can.
public class FriendlyNameAlreadyUsedException(string friendlyName)
    : Exception($"You already have a wallet called '{friendlyName}'.");