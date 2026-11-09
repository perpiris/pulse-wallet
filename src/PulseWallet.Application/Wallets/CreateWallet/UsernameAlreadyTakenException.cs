namespace PulseWallet.Application.Wallets.CreateWallet;

public class UsernameAlreadyTakenException(string username)
    : Exception($"Username '{username}' is already taken.");