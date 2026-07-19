namespace WhatsApp.Core.Client;

/// <summary>
/// Creates <see cref="IWhatsAppClient"/> instances for named WhatsApp accounts, for
/// applications that send messages from more than one WhatsApp Business phone number.
/// </summary>
public interface IWhatsAppClientFactory
{
    /// <summary>
    /// Creates a client bound to the given account.
    /// </summary>
    /// <param name="accountName">
    /// The account name previously registered via one of the
    /// <c>WhatsAppServiceCollectionExtensions.AddWhatsAppCore</c> overloads, or
    /// <see cref="Configuration.WhatsAppOptions.DefaultAccountName"/> for the default account.
    /// </param>
    /// <returns>A client for the given account.</returns>
    IWhatsAppClient CreateClient(string accountName);
}
