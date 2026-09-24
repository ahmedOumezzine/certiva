using Microsoft.AspNetCore.Routing;

namespace Certiva.Localization;

public sealed class SupportedCultureRouteConstraint : IRouteConstraint
{
    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        var culture = values[routeKey]?.ToString();
        return culture is "fr" or "en";
    }
}
