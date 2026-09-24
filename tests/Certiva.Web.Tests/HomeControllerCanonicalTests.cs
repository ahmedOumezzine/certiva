using Certiva.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Certiva.Web.Tests;

public sealed class HomeControllerCanonicalTests
{
    [Theory]
    [InlineData("/Home", "/fr")]
    [InlineData("/en/Home/Index", "/en")]
    public async Task IndexRedirectsLegacyHomePathsToCanonicalCulturePath(string path, string expectedPrefix)
    {
        var controller = CreateController(path);

        var result = await controller.Index(null, null);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.True(redirect.Permanent);
        Assert.Equal(expectedPrefix, redirect.Url);
    }

    [Fact]
    public void LegacyDetailsWithoutSlugReturnsNotFound()
    {
        var result = CreateController("/Home/Details").LegacyDetails(null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void LegacyDetailsWithSlugRedirectsToFrenchRoute()
    {
        var result = CreateController("/Home/Details").LegacyDetails("mon-examen");

        var redirect = Assert.IsType<RedirectToRouteResult>(result);
        Assert.True(redirect.Permanent);
        Assert.Equal("PublicExamDetails", redirect.RouteName);
        Assert.Equal("mon-examen", redirect.RouteValues!["slug"]);
        Assert.Equal("fr", redirect.RouteValues["culture"]);
    }

    [Fact]
    public void ErrorUsesTraceIdentifierAsRequestId()
    {
        var controller = CreateController("/error");
        controller.HttpContext.TraceIdentifier = "trace-123";

        var result = Assert.IsType<ViewResult>(controller.Error());

        Assert.Equal("trace-123", Assert.IsType<Certiva.Models.ErrorViewModel>(result.Model).RequestId);
    }

    private static HomeController CreateController(string path)
    {
        var controller = new HomeController(null!, null!, null!, null!, null!, null!, null!);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Path = path }
            },
            RouteData = new RouteData()
        };
        controller.RouteData.Values["culture"] = path.StartsWith("/en", StringComparison.OrdinalIgnoreCase) ? "en" : "fr";
        return controller;
    }
}
