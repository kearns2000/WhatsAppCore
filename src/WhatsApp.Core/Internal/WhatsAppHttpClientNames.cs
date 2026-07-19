namespace WhatsApp.Core.Internal;

/// <summary>
/// Computes the named <see cref="System.Net.Http.HttpClient"/> name associated with a given
/// WhatsApp account, so that every part of the library agrees on a single naming scheme.
/// </summary>
internal static class WhatsAppHttpClientNames
{
    private const string Prefix = "WhatsApp.Core:";

    /// <summary>
    /// Gets the named <see cref="System.Net.Http.HttpClient"/> name for the given account.
    /// </summary>
    /// <param name="accountName">The logical account name.</param>
    /// <returns>The name to use when registering or resolving the client via <see cref="System.Net.Http.IHttpClientFactory"/>.</returns>
    public static string For(string accountName) => Prefix + accountName;
}
