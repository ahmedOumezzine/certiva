using Certiva.Localization;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Certiva.Web.Tests;

public sealed class LocalizationConstraintTests
{
    [Theory]
    [InlineData("fr", true)]
    [InlineData("en", true)]
    [InlineData("nl", false)]
    [InlineData(null, false)]
    public void RouteConstraintAcceptsOnlySupportedCultures(string? culture, bool expected)
    {
        var values = new RouteValueDictionary();
        if (culture is not null) values["culture"] = culture;

        var result = new SupportedCultureRouteConstraint().Match(null, null, "culture", values, RouteDirection.IncomingRequest);

        Assert.Equal(expected, result);
    }
}
