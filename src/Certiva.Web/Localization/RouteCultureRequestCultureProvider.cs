using Microsoft.AspNetCore.Localization;

namespace Certiva.Localization;

public sealed class RouteCultureRequestCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var culture = httpContext.GetRouteData().Values["culture"]?.ToString();
        if (culture is null)
        {
            var firstPathSegment = httpContext.Request.Path.Value?
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            culture = firstPathSegment is "fr" or "en" ? firstPathSegment : null;
        }
        var cultureName = culture switch
        {
            "fr" => "fr-FR",
            "en" => "en-US",
            _ => null
        };

        return Task.FromResult(cultureName is null
            ? null
            : new ProviderCultureResult(cultureName, cultureName));
    }
}
