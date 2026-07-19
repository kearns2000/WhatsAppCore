using System.Text.Json.Nodes;
using WhatsApp.Core.Errors;

namespace WhatsApp.Core.Messages;

/// <summary>
/// A single contact card shared in a <see cref="ContactsMessageRequest"/>.
/// </summary>
public sealed record WhatsAppContact
{
    /// <summary>
    /// Gets the contact's name.
    /// </summary>
    public required WhatsAppContactName Name { get; init; }

    /// <summary>
    /// Gets the contact's birthday, formatted as <c>YYYY-MM-DD</c>.
    /// </summary>
    public string? Birthday { get; init; }

    /// <summary>
    /// Gets the contact's phone numbers.
    /// </summary>
    public IReadOnlyList<WhatsAppContactPhone>? Phones { get; init; }

    /// <summary>
    /// Gets the contact's email addresses.
    /// </summary>
    public IReadOnlyList<WhatsAppContactEmail>? Emails { get; init; }

    /// <summary>
    /// Gets the contact's organization details.
    /// </summary>
    public WhatsAppContactOrganization? Organization { get; init; }

    /// <summary>
    /// Gets the contact's postal addresses.
    /// </summary>
    public IReadOnlyList<WhatsAppContactAddress>? Addresses { get; init; }

    /// <summary>
    /// Gets the contact's associated URLs.
    /// </summary>
    public IReadOnlyList<string>? Urls { get; init; }

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name.FormattedName))
        {
            throw new WhatsAppValidationException("A contact's formatted name must not be empty.");
        }
    }

    internal JsonObject ToJson()
    {
        var obj = new JsonObject { ["name"] = Name.ToJson() };

        if (!string.IsNullOrEmpty(Birthday))
        {
            obj["birthday"] = Birthday;
        }

        if (Phones is { Count: > 0 })
        {
            obj["phones"] = ToArray(Phones, p => p.ToJson());
        }

        if (Emails is { Count: > 0 })
        {
            obj["emails"] = ToArray(Emails, e => e.ToJson());
        }

        if (Organization is not null)
        {
            obj["org"] = Organization.ToJson();
        }

        if (Addresses is { Count: > 0 })
        {
            obj["addresses"] = ToArray(Addresses, a => a.ToJson());
        }

        if (Urls is { Count: > 0 })
        {
            obj["urls"] = ToArray(Urls, url => new JsonObject { ["url"] = url });
        }

        return obj;
    }

    private static JsonArray ToArray<T>(IEnumerable<T> items, Func<T, JsonObject> selector)
    {
        var array = new JsonArray();
        foreach (var item in items)
        {
            array.Add(selector(item));
        }

        return array;
    }
}

/// <summary>
/// The structured name of a <see cref="WhatsAppContact"/>.
/// </summary>
public sealed record WhatsAppContactName
{
    /// <summary>Gets the full, formatted display name. Required.</summary>
    public required string FormattedName { get; init; }

    /// <summary>Gets the given (first) name.</summary>
    public string? FirstName { get; init; }

    /// <summary>Gets the family (last) name.</summary>
    public string? LastName { get; init; }

    /// <summary>Gets the middle name.</summary>
    public string? MiddleName { get; init; }

    /// <summary>Gets the honorific prefix (e.g. "Dr.").</summary>
    public string? Prefix { get; init; }

    /// <summary>Gets the honorific suffix (e.g. "Jr.").</summary>
    public string? Suffix { get; init; }

    internal JsonObject ToJson()
    {
        var obj = new JsonObject { ["formatted_name"] = FormattedName };
        if (FirstName is not null)
        {
            obj["first_name"] = FirstName;
        }

        if (LastName is not null)
        {
            obj["last_name"] = LastName;
        }

        if (MiddleName is not null)
        {
            obj["middle_name"] = MiddleName;
        }

        if (Prefix is not null)
        {
            obj["prefix"] = Prefix;
        }

        if (Suffix is not null)
        {
            obj["suffix"] = Suffix;
        }

        return obj;
    }
}

/// <summary>
/// A phone number associated with a <see cref="WhatsAppContact"/>.
/// </summary>
public sealed record WhatsAppContactPhone
{
    /// <summary>Gets the phone number, as displayed (formatting characters allowed).</summary>
    public required string Phone { get; init; }

    /// <summary>Gets the label for this phone number (e.g. "HOME", "WORK", "CELL").</summary>
    public string? Type { get; init; }

    /// <summary>Gets the WhatsApp id associated with this number, if known.</summary>
    public string? WaId { get; init; }

    internal JsonObject ToJson()
    {
        var obj = new JsonObject { ["phone"] = Phone };
        if (Type is not null)
        {
            obj["type"] = Type;
        }

        if (WaId is not null)
        {
            obj["wa_id"] = WaId;
        }

        return obj;
    }
}

/// <summary>
/// An email address associated with a <see cref="WhatsAppContact"/>.
/// </summary>
public sealed record WhatsAppContactEmail
{
    /// <summary>Gets the email address.</summary>
    public required string Email { get; init; }

    /// <summary>Gets the label for this email address (e.g. "HOME", "WORK").</summary>
    public string? Type { get; init; }

    internal JsonObject ToJson()
    {
        var obj = new JsonObject { ["email"] = Email };
        if (Type is not null)
        {
            obj["type"] = Type;
        }

        return obj;
    }
}

/// <summary>
/// Organization details associated with a <see cref="WhatsAppContact"/>.
/// </summary>
public sealed record WhatsAppContactOrganization
{
    /// <summary>Gets the company name.</summary>
    public string? Company { get; init; }

    /// <summary>Gets the department name.</summary>
    public string? Department { get; init; }

    /// <summary>Gets the job title.</summary>
    public string? Title { get; init; }

    internal JsonObject ToJson()
    {
        var obj = new JsonObject();
        if (Company is not null)
        {
            obj["company"] = Company;
        }

        if (Department is not null)
        {
            obj["department"] = Department;
        }

        if (Title is not null)
        {
            obj["title"] = Title;
        }

        return obj;
    }
}

/// <summary>
/// A postal address associated with a <see cref="WhatsAppContact"/>.
/// </summary>
public sealed record WhatsAppContactAddress
{
    /// <summary>Gets the street address.</summary>
    public string? Street { get; init; }

    /// <summary>Gets the city.</summary>
    public string? City { get; init; }

    /// <summary>Gets the state or province.</summary>
    public string? State { get; init; }

    /// <summary>Gets the postal or ZIP code.</summary>
    public string? Zip { get; init; }

    /// <summary>Gets the country name.</summary>
    public string? Country { get; init; }

    /// <summary>Gets the two-letter country code.</summary>
    public string? CountryCode { get; init; }

    /// <summary>Gets the label for this address (e.g. "HOME", "WORK").</summary>
    public string? Type { get; init; }

    internal JsonObject ToJson()
    {
        var obj = new JsonObject();
        if (Street is not null)
        {
            obj["street"] = Street;
        }

        if (City is not null)
        {
            obj["city"] = City;
        }

        if (State is not null)
        {
            obj["state"] = State;
        }

        if (Zip is not null)
        {
            obj["zip"] = Zip;
        }

        if (Country is not null)
        {
            obj["country"] = Country;
        }

        if (CountryCode is not null)
        {
            obj["country_code"] = CountryCode;
        }

        if (Type is not null)
        {
            obj["type"] = Type;
        }

        return obj;
    }
}
