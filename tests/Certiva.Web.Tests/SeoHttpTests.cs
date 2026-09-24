using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Certiva.Domain.Exams;
using Certiva.Domain.Attempts;
using Certiva.Domain.Enums;
using Certiva.Infrastructure.Data;
using Certiva.Services.Exams;
using Certiva.Application.Attempts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Certiva.Web.Tests;

public sealed class SeoHttpTests
{
    [Theory]
    [InlineData("/fr", "https://seo.test/fr")]
    [InlineData("/en", "https://seo.test/en")]
    [InlineData("/fr/examens", "https://seo.test/fr/examens")]
    [InlineData("/en/exams", "https://seo.test/en/exams")]
    public async Task Home_and_catalog_pages_use_their_own_https_canonical(string path, string expectedCanonical)
    {
        using var factory = new SeoWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://request.test") });

        var html = await client.GetStringAsync(path);

        Assert.Contains($"<link rel=\"canonical\" href=\"{expectedCanonical}\"", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/fr", "https://seo.test/fr", "https://seo.test/en")]
    [InlineData("/fr/examens", "https://seo.test/fr/examens", "https://seo.test/en/exams")]
    public async Task French_home_and_catalog_advertise_reciprocal_english_alternates(string path, string french, string english)
    {
        using var factory = new SeoWebApplicationFactory();
        using var client = factory.CreateClient();

        var frenchHtml = await client.GetStringAsync(path);
        var englishPath = path == "/fr" ? "/en" : "/en/exams";
        var englishHtml = await client.GetStringAsync(englishPath);

        Assert.Contains($"hreflang=\"fr\" href=\"{french}\"", frenchHtml, StringComparison.Ordinal);
        Assert.Contains($"hreflang=\"en\" href=\"{english}\"", frenchHtml, StringComparison.Ordinal);
        Assert.Contains($"hreflang=\"x-default\" href=\"{french}\"", frenchHtml, StringComparison.Ordinal);
        Assert.Contains($"hreflang=\"fr\" href=\"{french}\"", englishHtml, StringComparison.Ordinal);
        Assert.Contains($"hreflang=\"en\" href=\"{english}\"", englishHtml, StringComparison.Ordinal);
        Assert.Contains($"hreflang=\"x-default\" href=\"{french}\"", englishHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Error_page_is_noindex_and_has_no_canonical_alternates_or_json_ld()
    {
        using var factory = new SeoWebApplicationFactory();
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/Home/Error");

        Assert.Contains("<title>Une erreur est survenue | Certiva</title>", html, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"robots\" content=\"noindex,nofollow\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("rel=\"canonical\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("hreflang=", html, StringComparison.Ordinal);
        Assert.DoesNotContain("application/ld+json", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_take_and_result_pages_are_noindex_nofollow()
    {
        using var factory = new SeoWebApplicationFactory();
        using var client = factory.CreateClient();
        using var loginResponse = await client.GetAsync("/fr/login");
        var login = await loginResponse.Content.ReadAsStringAsync();
        Assert.True(loginResponse.IsSuccessStatusCode, $"Login returned {(int)loginResponse.StatusCode}.");
        AssertNoIndexNofollow(login);

        using var scope = factory.Services.CreateScope();
        var submit = scope.ServiceProvider.GetRequiredService<Certiva.Application.Attempts.IExamAttemptSubmitService>();
        var start = scope.ServiceProvider.GetRequiredService<IExamAttemptStartService>();
        var started = await start.StartAsync(new StartAttemptRequest("seo-exam-fr", null, false, "guest@example.com"));
        Assert.NotNull(started);
        client.DefaultRequestHeaders.Add("Cookie", $"ExamsApp.Attempt.{started!.AttemptId:N}={started.AccessToken}");

        var take = await client.GetStringAsync($"/fr/examens/seo-exam-fr/passer/{started.AttemptId}");
        AssertNoIndexNofollow(take);
        Assert.NotNull(await submit.SubmitAsync(started.AttemptId, null, started.AccessToken));
        var result = await client.GetStringAsync($"/fr/examens/seo-exam-fr/resultat/{started.AttemptId}");
        AssertNoIndexNofollow(result);
    }

    [Fact]
    public async Task English_fallback_is_noindex_and_does_not_advertise_a_missing_translation()
    {
        using var factory = new SeoWebApplicationFactory(includeEnglishTranslation: false);
        using var client = factory.CreateClient();

        var englishHome = await client.GetStringAsync("/en");
        var frenchHome = await client.GetStringAsync("/fr");
        var englishDetail = await client.GetStringAsync("/en/exams/seo-exam-fr");

        AssertNoIndex(englishHome);
        Assert.DoesNotContain("rel=\"canonical\"", englishHome, StringComparison.Ordinal);
        Assert.DoesNotContain("hreflang=\"en\"", frenchHome, StringComparison.Ordinal);
        AssertNoIndex(englishDetail);
        Assert.DoesNotContain("rel=\"canonical\"", englishDetail, StringComparison.Ordinal);

        var sitemap = XDocument.Parse(await client.GetStringAsync("/sitemap.xml"));
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var locations = sitemap.Root!.Elements(ns + "url").Select(node => (string)node.Element(ns + "loc")!).ToArray();
        Assert.DoesNotContain("https://seo.test/en", locations);
        Assert.DoesNotContain("https://seo.test/en/exams", locations);
        Assert.DoesNotContain("https://seo.test/en/exams/seo-exam-en", locations);
    }

    [Theory]
    [InlineData("/fr/examens/seo-exam-fr", "https://seo.test/fr/examens/seo-exam-fr", "https://seo.test/en/exams/seo-exam-en")]
    [InlineData("/en/exams/seo-exam-en", "https://seo.test/en/exams/seo-exam-en", "https://seo.test/fr/examens/seo-exam-fr")]
    public async Task Exam_details_use_local_canonical_and_reciprocal_hreflang(string path, string canonical, string otherLanguage)
    {
        using var factory = new SeoWebApplicationFactory();
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync(path);

        Assert.Contains($"<link rel=\"canonical\" href=\"{canonical}\"", html, StringComparison.Ordinal);
        Assert.Contains($"hreflang=\"fr\" href=\"https://seo.test/fr/examens/seo-exam-fr\"", html, StringComparison.Ordinal);
        Assert.Contains($"hreflang=\"en\" href=\"https://seo.test/en/exams/seo-exam-en\"", html, StringComparison.Ordinal);
        Assert.Contains($"hreflang=\"x-default\" href=\"https://seo.test/fr/examens/seo-exam-fr\"", html, StringComparison.Ordinal);
        Assert.Contains(otherLanguage, html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Detail_language_switch_links_keep_the_matching_localized_slug()
    {
        using var factory = new SeoWebApplicationFactory();
        using var client = factory.CreateClient();

        var french = await client.GetStringAsync("/fr/examens/seo-exam-fr");
        var english = await client.GetStringAsync("/en/exams/seo-exam-en");

        Assert.Contains("lang=\"en\" href=\"/en/exams/seo-exam-en\"", french, StringComparison.Ordinal);
        Assert.Contains("lang=\"fr\" href=\"/fr/examens/seo-exam-fr\"", english, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sitemap_contains_only_public_published_urls_and_matches_canonicals()
    {
        using var factory = new SeoWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/sitemap.xml");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = XDocument.Parse(await response.Content.ReadAsStringAsync());
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var locations = document.Root!.Elements(ns + "url")
            .Select(node => (string)node.Element(ns + "loc")!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("https://seo.test/fr", locations);
        Assert.Contains("https://seo.test/en", locations);
        Assert.Contains("https://seo.test/fr/examens", locations);
        Assert.Contains("https://seo.test/en/exams", locations);
        Assert.Contains("https://seo.test/fr/examens/seo-exam-fr", locations);
        Assert.Contains("https://seo.test/en/exams/seo-exam-en", locations);
        Assert.DoesNotContain(locations, url => Regex.IsMatch(url, "/(start|take|result|resultat|login|Admin|Error)(/|$)", RegexOptions.IgnoreCase));
        Assert.All(locations, url => Assert.StartsWith("https://seo.test/", url, StringComparison.Ordinal));

        foreach (var path in new[] { "/fr", "/en", "/fr/examens", "/en/exams", "/fr/examens/seo-exam-fr", "/en/exams/seo-exam-en" })
        {
            var html = await client.GetStringAsync(path);
            var canonical = Regex.Match(html, @"<link rel=""canonical"" href=""([^""]+)""").Groups[1].Value;
            Assert.Contains(canonical, locations);
        }
    }

    [Fact]
    public async Task Robots_allows_crawling_so_robots_meta_can_be_read()
    {
        using var factory = new SeoWebApplicationFactory();
        using var client = factory.CreateClient();

        var robots = await client.GetStringAsync("/robots.txt");

        Assert.Contains("Allow: /", robots, StringComparison.Ordinal);
        Assert.DoesNotContain("Disallow:", robots, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sitemap: https://seo.test/sitemap.xml", robots, StringComparison.Ordinal);
    }

    private static void AssertNoIndex(string html)
    {
        Assert.Contains("<meta name=\"robots\" content=\"noindex", html, StringComparison.Ordinal);
        Assert.DoesNotContain("rel=\"canonical\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("hreflang=", html, StringComparison.Ordinal);
        Assert.DoesNotContain("application/ld+json", html, StringComparison.Ordinal);
    }

    private static void AssertNoIndexNofollow(string html)
    {
        Assert.Contains("<meta name=\"robots\" content=\"noindex,nofollow\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("rel=\"canonical\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("hreflang=", html, StringComparison.Ordinal);
        Assert.DoesNotContain("application/ld+json", html, StringComparison.Ordinal);
    }
}

internal sealed class SeoWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly bool _includeEnglishTranslation;
    private readonly string _databaseName = $"seo-http-{Guid.NewGuid():N}";

    public SeoWebApplicationFactory(bool includeEnglishTranslation = true) => _includeEnglishTranslation = includeEnglishTranslation;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=unused;Database=CertivaTests;Trusted_Connection=True;",
            ["Seo:BaseUrl"] = "https://seo.test"
        }));
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        SeedExam(db, _includeEnglishTranslation);
    }

    private static void SeedExam(ApplicationDbContext db, bool includeEnglishTranslation)
    {
        var examId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var yesChoiceId = Guid.NewGuid();
        var noChoiceId = Guid.NewGuid();
        var exam = new Exam
        {
            Id = examId, Name = "Examen SEO", Description = "Description française complète.", Code = "SEO01",
            Slug = "seo-exam-fr", Status = Status.Published, DurationMinutes = 30, PassingPercentage = 70,
            QuestionsCount = 1,
            Skills = [new Skill
            {
                Id = skillId, ExamId = examId, Name = "Compétence", Description = "Description compétence.", Pourcentage = 100,
                Translations = includeEnglishTranslation ? [new SkillTranslation { Id = Guid.NewGuid(), SkillId = skillId, Culture = "en", Name = "Skill", Description = "Skill description." }] : []
            }],
            Questions = [new Question
            {
                Id = questionId, ExamId = examId, SkillId = skillId, Description = "Quelle réponse ?", Explication = "Explication.",
                QuestionType = QuestionType.SingleChoice, Status = Status.Published,
                Translations = includeEnglishTranslation ? [new QuestionTranslation { Id = Guid.NewGuid(), QuestionId = questionId, Culture = "en", Description = "Which answer?", Explanation = "Explanation." }] : [],
                Choices = [
                    new Choice { Id = yesChoiceId, ChoiceText = "Oui", IsCorrect = true,
                        Translations = includeEnglishTranslation ? [new ChoiceTranslation { Id = Guid.NewGuid(), ChoiceId = yesChoiceId, Culture = "en", ChoiceText = "Yes" }] : [] },
                    new Choice { Id = noChoiceId, ChoiceText = "Non", IsCorrect = false,
                        Translations = includeEnglishTranslation ? [new ChoiceTranslation { Id = Guid.NewGuid(), ChoiceId = noChoiceId, Culture = "en", ChoiceText = "No" }] : [] }
                ]
            }],
            Translations = includeEnglishTranslation ? [new ExamTranslation
            {
                Id = Guid.NewGuid(), ExamId = examId, Culture = "en", Name = "SEO exam", Description = "Complete English description.", Slug = "seo-exam-en"
            }] : []
        };
        db.Exams.AddRange(exam,
            new Exam { Id = Guid.NewGuid(), Name = "Draft exam", Description = "Draft", Code = "DRAFT1", Slug = "draft-exam", Status = Status.Draft },
            new Exam { Id = Guid.NewGuid(), Name = "Archived exam", Description = "Archived", Code = "ARCH01", Slug = "archived-exam", Status = Status.Archived });
        db.SaveChanges();
    }
}
