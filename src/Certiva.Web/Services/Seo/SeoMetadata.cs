using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Certiva.Services.Seo;

public static partial class SeoMetadata
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string AbsoluteUrl(HttpRequest request, string path, IConfiguration? configuration = null)
    {
        var configuredBaseUrl = configuration?["Seo:BaseUrl"]?.Trim().TrimEnd('/');
        var origin = Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var configuredUri)
            ? configuredUri.GetLeftPart(UriPartial.Authority)
            : $"{request.Scheme}://{request.Host}";

        return $"{origin}/{path.TrimStart('/')}";
    }

    public static string CleanDescription(string? value, int maxLength = 160)
    {
        var text = WebUtility.HtmlDecode(value ?? string.Empty);
        text = MarkupRegex().Replace(text, " ");
        text = WhitespaceRegex().Replace(text, " ").Trim();

        if (text.Length <= maxLength)
            return text;

        var truncated = text[..(maxLength - 1)];
        var lastSpace = truncated.LastIndexOf(' ');
        return $"{(lastSpace > maxLength / 2 ? truncated[..lastSpace] : truncated).TrimEnd()}…";
    }

    public static string ToJson(object value) => JsonSerializer.Serialize(value, JsonOptions);

    [GeneratedRegex("<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex MarkupRegex();

    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
