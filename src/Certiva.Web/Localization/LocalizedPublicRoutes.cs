namespace Certiva.Localization;

public static class LocalizedPublicRoutes
{
    public static Microsoft.AspNetCore.Routing.RouteValueDictionary Values(
        string culture,
        params (string Name, object? Value)[] values)
    {
        var routeValues = new Microsoft.AspNetCore.Routing.RouteValueDictionary();
        if (culture != "en")
            routeValues["culture"] = "fr";

        foreach (var (name, value) in values)
            routeValues[name] = value;

        return routeValues;
    }

    public static string Details(string culture) => culture == "en"
        ? "PublicExamDetailsEnglish"
        : "PublicExamDetails";

    public static string DetailsPath(string culture, string slug) => culture == "en"
        ? $"/en/exams/{Uri.EscapeDataString(slug)}"
        : $"/fr/examens/{Uri.EscapeDataString(slug)}";

    public static string Start(string culture) => culture == "en"
        ? "PublicExamStartEnglish"
        : "PublicExamStart";

    public static string Take(string culture) => culture == "en"
        ? "PublicExamTakeEnglish"
        : "PublicExamTake";

    public static string Result(string culture) => culture == "en"
        ? "PublicExamResultEnglish"
        : "PublicExamResult";

    public static string CultureFromPath(string? path) =>
        path?.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() == "en"
            ? "en"
            : "fr";

    public static string SwitchPath(string? path, string culture)
    {
        var segments = path?.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList() ?? [];
        if (segments.Count > 0 && segments[0] is "fr" or "en")
            segments.RemoveAt(0);

        if (segments.Count == 0)
            return $"/{culture}";

        if (segments[0] is "examens" or "exams")
        {
            segments[0] = culture == "en" ? "exams" : "examens";
            if (segments.Count > 2)
            {
                segments[2] = segments[2] switch
                {
                    "commencer" or "start" => culture == "en" ? "start" : "commencer",
                    "passer" or "take" => culture == "en" ? "take" : "passer",
                    "resultat" or "result" => culture == "en" ? "result" : "resultat",
                    _ => segments[2]
                };
            }
        }

        return $"/{culture}/{string.Join('/', segments)}";
    }
}
