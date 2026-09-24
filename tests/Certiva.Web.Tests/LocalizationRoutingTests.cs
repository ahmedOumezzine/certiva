using Certiva.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;
using Xunit;
using System.Globalization;
using System.Resources;

namespace Certiva.Web.Tests;

public sealed class LocalizationRoutingTests
{
    [Fact]
    public void Shared_resources_contain_translations_for_both_supported_languages()
    {
        var resources = new ResourceManager(
            "Certiva.Resources.Localization.SharedResource",
            typeof(SharedResource).Assembly);

        Assert.Equal("Accueil", resources.GetString("Accueil", CultureInfo.GetCultureInfo("fr-FR")));
        Assert.Equal("Home", resources.GetString("Accueil", CultureInfo.GetCultureInfo("en-US")));
        Assert.Equal("Published", resources.GetString("Published", CultureInfo.GetCultureInfo("en-US")));
    }

    [Theory]
    [InlineData("fr", "fr-FR")]
    [InlineData("en", "en-US")]
    public async Task Route_provider_maps_supported_culture(string routeCulture, string expectedCulture)
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IRoutingFeature>(new RoutingFeature
        {
            RouteData = new RouteData(new RouteValueDictionary { ["culture"] = routeCulture })
        });

        var result = await new RouteCultureRequestCultureProvider()
            .DetermineProviderCultureResult(context);

        Assert.NotNull(result);
        Assert.Equal(expectedCulture, result!.Cultures.Single());
        Assert.Equal(expectedCulture, result.UICultures.Single());
    }

    [Fact]
    public async Task Route_provider_does_not_override_when_route_has_no_supported_culture()
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IRoutingFeature>(new RoutingFeature
        {
            RouteData = new RouteData(new RouteValueDictionary { ["culture"] = "de" })
        });

        var result = await new RouteCultureRequestCultureProvider()
            .DetermineProviderCultureResult(context);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("fr", true)]
    [InlineData("en", true)]
    [InlineData("de", false)]
    [InlineData(null, false)]
    public void Route_constraint_accepts_only_supported_segments(string? culture, bool expected)
    {
        var values = new RouteValueDictionary();
        if (culture is not null)
        {
            values["culture"] = culture;
        }

        var result = new SupportedCultureRouteConstraint()
            .Match(null, null, "culture", values, RouteDirection.IncomingRequest);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("/fr", "en", "/en")]
    [InlineData("/fr/examens", "en", "/en/exams")]
    [InlineData("/fr/examens/sql", "en", "/en/exams/sql")]
    [InlineData("/fr/examens/sql/commencer", "en", "/en/exams/sql/start")]
    [InlineData("/fr/examens/sql/passer/00000000-0000-0000-0000-000000000001", "en", "/en/exams/sql/take/00000000-0000-0000-0000-000000000001")]
    [InlineData("/fr/examens/sql/resultat/00000000-0000-0000-0000-000000000001", "en", "/en/exams/sql/result/00000000-0000-0000-0000-000000000001")]
    [InlineData("/en/exams/sql/result/00000000-0000-0000-0000-000000000001", "fr", "/fr/examens/sql/resultat/00000000-0000-0000-0000-000000000001")]
    public void Language_switch_maps_public_route_segments(string path, string culture, string expected)
    {
        Assert.Equal(expected, LocalizedPublicRoutes.SwitchPath(path, culture));
    }

    [Theory]
    [InlineData("fr", "PublicExamDetails", "PublicExamStart", "PublicExamTake", "PublicExamResult")]
    [InlineData("en", "PublicExamDetailsEnglish", "PublicExamStartEnglish", "PublicExamTakeEnglish", "PublicExamResultEnglish")]
    public void Public_route_names_follow_selected_language(
        string culture,
        string details,
        string start,
        string take,
        string result)
    {
        Assert.Equal(details, LocalizedPublicRoutes.Details(culture));
        Assert.Equal(start, LocalizedPublicRoutes.Start(culture));
        Assert.Equal(take, LocalizedPublicRoutes.Take(culture));
        Assert.Equal(result, LocalizedPublicRoutes.Result(culture));
    }

    [Theory]
    [InlineData("fr", "bases-de-donnees-sql", "/fr/examens/bases-de-donnees-sql")]
    [InlineData("en", "sql-databases", "/en/exams/sql-databases")]
    [InlineData("en", "slug with spaces", "/en/exams/slug%20with%20spaces")]
    public void Localized_detail_urls_use_the_correct_language_route_and_slug(
        string culture,
        string slug,
        string expected)
    {
        Assert.Equal(expected, LocalizedPublicRoutes.DetailsPath(culture, slug));
    }

    [Fact]
    public void French_named_route_values_include_required_culture_without_polluting_english_paths()
    {
        var french = LocalizedPublicRoutes.Values("fr", ("slug", "sql"), ("attemptId", Guid.Empty));
        var english = LocalizedPublicRoutes.Values("en", ("slug", "sql"), ("attemptId", Guid.Empty));

        Assert.Equal("fr", french["culture"]);
        Assert.False(english.ContainsKey("culture"));
    }
}
