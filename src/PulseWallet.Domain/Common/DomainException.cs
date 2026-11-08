namespace PulseWallet.Domain.Common;

/// <summary>
/// Thrown when an operation would break a business rule (for example an invalid username).
/// The message is written for the client: the API turns this exception into a 400 response
/// and shows the message, unlike unexpected exceptions, which become a generic 500.
/// </summary>
public class DomainException(string message) : Exception(message);