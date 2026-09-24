using Microsoft.Extensions.Localization;

namespace Certiva.Localization;

public static class EnumLocalizerExtensions
{
    public static string EnumLabel(this IStringLocalizer<SharedResource> localizer, Enum value) =>
        localizer[value.ToString()].Value;
}
