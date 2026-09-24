using System.Globalization;
using System.Resources;

namespace Certiva.Localization;

/// <summary>
/// Strongly-typed, culture-aware resource access for DataAnnotations.
/// Unlike IStringLocalizer, DataAnnotations resource attributes require
/// public static properties on their ErrorMessageResourceType.
/// </summary>
public static class ValidationResources
{
    private static readonly ResourceManager ResourceManager = new(
        "Certiva.Resources.Localization.SharedResource",
        typeof(SharedResource).Assembly);

    public static string LoginEmailRequired => Get(nameof(LoginEmailRequired));
    public static string ValidEmailRequired => Get(nameof(ValidEmailRequired));
    public static string LoginPasswordRequired => Get(nameof(LoginPasswordRequired));
    public static string GuestEmailTooLong => Get(nameof(GuestEmailTooLong));

    private static string Get(string key) =>
        ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
