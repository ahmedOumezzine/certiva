using System.Text.Json;
using Certiva.Services.Seo;
using Xunit;

namespace Certiva.Web.Tests;

public sealed class SeoMetadataTests
{
    [Fact]
    public void ToJson_ProducesValidEscapedJsonLd()
    {
        var json = SeoMetadata.ToJson(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "LearningResource",
            ["name"] = "Exam </script><script>alert(1)</script>"
        });

        using var document = JsonDocument.Parse(json);

        Assert.Equal("https://schema.org", document.RootElement.GetProperty("@context").GetString());
        Assert.Equal("Exam </script><script>alert(1)</script>", document.RootElement.GetProperty("name").GetString());
        Assert.DoesNotContain("</script>", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CleanDescription_RemovesMarkupAndTruncatesAtAWordBoundary()
    {
        var description = SeoMetadata.CleanDescription("<p>Un examen pratique avec beaucoup de contenu pour préparer les certifications et améliorer votre niveau technique efficacement.</p>", 65);

        Assert.DoesNotContain("<", description);
        Assert.True(description.Length <= 65);
        Assert.EndsWith("…", description);
    }
}
