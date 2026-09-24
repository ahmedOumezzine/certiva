using Certiva.Domain.Enums;
using Certiva.Models;
using Certiva.Models.Exams;
using Certiva.Services.Exams;
using ApplicationCatalogService = Certiva.Application.Exams.IExamCatalogService;
using AttemptReadService = Certiva.Application.Attempts.IExamAttemptReadService;
using AttemptAutosaveService = Certiva.Application.Attempts.IExamAttemptAutosaveService;
using AttemptStartService = Certiva.Application.Attempts.IExamAttemptStartService;
using AttemptSubmitService = Certiva.Application.Attempts.IExamAttemptSubmitService;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.Extensions.Localization;
using Certiva.Localization;
using Certiva.Services.Seo;

namespace Certiva.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationCatalogService _catalog;
    private readonly AttemptReadService _attemptReads;
    private readonly AttemptAutosaveService _attemptAutosave;
    private readonly AttemptStartService _attemptStart;
    private readonly AttemptSubmitService _attemptSubmit;
    private readonly IStringLocalizer<SharedResource> _text;
    private readonly IConfiguration _configuration;

    public HomeController(
        ApplicationCatalogService catalog,
        AttemptReadService attemptReads,
        AttemptAutosaveService attemptAutosave,
        AttemptStartService attemptStart,
        AttemptSubmitService attemptSubmit,
        IStringLocalizer<SharedResource> text,
        IConfiguration configuration)
    {
        _catalog = catalog;
        _attemptReads = attemptReads;
        _attemptAutosave = attemptAutosave;
        _attemptStart = attemptStart;
        _attemptSubmit = attemptSubmit;
        _text = text;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index(string? search, Guid? skillId, int page = 1, int pageSize = 9)
    {
        var requestPath = Request.Path.Value?.TrimEnd('/');
        if (requestPath is not null
            && (requestPath.Equals("/Home", StringComparison.OrdinalIgnoreCase)
                || requestPath.Equals("/Home/Index", StringComparison.OrdinalIgnoreCase)
                || requestPath.Equals("/fr/Home", StringComparison.OrdinalIgnoreCase)
                || requestPath.Equals("/fr/Home/Index", StringComparison.OrdinalIgnoreCase)
                || requestPath.Equals("/en/Home", StringComparison.OrdinalIgnoreCase)
                || requestPath.Equals("/en/Home/Index", StringComparison.OrdinalIgnoreCase)))
        {
            var canonicalCulture = RouteData.Values["culture"]?.ToString() is "en" ? "en"
                : requestPath.StartsWith("/en/", StringComparison.OrdinalIgnoreCase) ? "en" : "fr";
            return RedirectPermanent($"/{canonicalCulture}{Request.QueryString}");
        }

        var culture = RouteCulture();
        var catalogDto = await _catalog.GetExamListAsync(search, skillId, page, pageSize, culture);
        var model = catalogDto.ToViewModel();
        var isCatalogPath = requestPath is "/fr/examens" or "/en/exams";
        var canonicalPath = isCatalogPath
            ? culture == "en" ? "/en/exams" : "/fr/examens"
            : $"/{culture}";
        var description = _text["seo.home.description"].Value;

        ViewData["Title"] = _text["seo.home.title"].Value;
        ViewData["DocumentTitle"] = ViewData["Title"];
        ViewData["MetaDescription"] = description;
        ViewData["CanonicalUrl"] = SeoMetadata.AbsoluteUrl(Request, canonicalPath, _configuration);
        var hasFilters = !string.IsNullOrWhiteSpace(model.Search) || model.SkillId.HasValue;
        var hasEnglishFallback = culture == "en" &&
            (model.Exams.Any(exam => exam.HasEnglishFallback) || model.Skills.Any(skill => skill.HasEnglishFallback));
        var hasCompleteEnglishCatalog = !hasFilters &&
            await _catalog.HasCompleteEnglishCatalogAsync();
        hasEnglishFallback |= culture == "en" && !hasCompleteEnglishCatalog;
        if (hasEnglishFallback)
        {
            ViewData["ShowEnglishFallbackNotice"] = true;
            ViewData["Robots"] = "noindex,follow";
        }

        if (hasFilters)
        {
            ViewData["Robots"] = "noindex,follow";
        }
        else if (model.Page > 1)
        {
            ViewData["CanonicalUrl"] = SeoMetadata.AbsoluteUrl(Request, $"{canonicalPath}?page={model.Page}", _configuration);
            ViewData["Title"] = _text["seo.home.page", model.Page].Value;
            ViewData["DocumentTitle"] = $"{ViewData["Title"]} | Certiva";
        }

        if (!hasFilters && model.Page == 1)
        {
            var alternateLanguages = new Dictionary<string, string>
            {
                ["fr"] = SeoMetadata.AbsoluteUrl(Request, isCatalogPath ? "/fr/examens" : "/fr", _configuration),
                ["x-default"] = SeoMetadata.AbsoluteUrl(Request, isCatalogPath ? "/fr/examens" : "/fr", _configuration)
            };
            if (hasCompleteEnglishCatalog)
                alternateLanguages["en"] = SeoMetadata.AbsoluteUrl(Request, isCatalogPath ? "/en/exams" : "/en", _configuration);
            ViewData["AlternateLanguages"] = alternateLanguages;
            var homeUrl = SeoMetadata.AbsoluteUrl(Request, canonicalPath, _configuration);
            var logoUrl = SeoMetadata.AbsoluteUrl(Request, "/images/branding/certiva-logo.png", _configuration);
            ViewData["JsonLd"] = SeoMetadata.ToJson(new object[]
            {
                new Dictionary<string, object?>
                {
                    ["@context"] = "https://schema.org",
                    ["@type"] = "Organization",
                    ["name"] = "Certiva",
                    ["url"] = SeoMetadata.AbsoluteUrl(Request, "/", _configuration),
                    ["logo"] = logoUrl
                },
                new Dictionary<string, object?>
                {
                    ["@context"] = "https://schema.org",
                    ["@type"] = "WebSite",
                    ["name"] = "Certiva",
                    ["url"] = homeUrl,
                    ["inLanguage"] = culture
                }
            });
        }

        return View(model);
    }

    [HttpGet("/{culture:culture}/examens/{slug}", Name = "PublicExamDetails")]
    [HttpGet("/en/exams/{slug}", Name = "PublicExamDetailsEnglish")]
    [HttpGet("/examens/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var culture = RouteCulture();
        var detailsDto = await _catalog.GetDetailsAsync(slug, culture);
        var model = detailsDto?.ToViewModel();
        if (model == null)
            return NotFound();

        if (!string.Equals(slug, model.Slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToRoutePermanent(LocalizedPublicRoutes.Details(culture),
                LocalizedPublicRoutes.Values(culture, ("slug", model.Slug)));
        }

        var isEnglish = culture == "en";
        var frenchPath = LocalizedPublicRoutes.DetailsPath("fr", model.FrenchSlug);
        var englishPath = LocalizedPublicRoutes.DetailsPath("en", model.EnglishSlug);
        var canonicalPath = isEnglish ? englishPath : frenchPath;
        ViewData["LanguageSwitchUrls"] = new Dictionary<string, string>
        {
            ["fr"] = frenchPath,
            ["en"] = englishPath
        };
        ViewData["Title"] = _text["seo.details.title", model.Name].Value;
        ViewData["DocumentTitle"] = $"{ViewData["Title"]} | Certiva";
        ViewData["MetaDescription"] = SeoMetadata.CleanDescription(model.Description);
        if (model.HasEnglishFallback)
            ViewData["ShowEnglishFallbackNotice"] = true;

        if (isEnglish && !model.HasCompleteTranslation)
        {
            ViewData["Robots"] = "noindex,follow";
        }
        else
        {
            var canonicalUrl = SeoMetadata.AbsoluteUrl(Request, canonicalPath, _configuration);
            ViewData["CanonicalUrl"] = canonicalUrl;
            var alternateLanguages = new Dictionary<string, string>
            {
                ["fr"] = SeoMetadata.AbsoluteUrl(Request, frenchPath, _configuration),
                ["x-default"] = SeoMetadata.AbsoluteUrl(Request, frenchPath, _configuration)
            };
            if (model.HasCompleteEnglishTranslation)
                alternateLanguages["en"] = SeoMetadata.AbsoluteUrl(Request, englishPath, _configuration);
            ViewData["AlternateLanguages"] = alternateLanguages;
            ViewData["JsonLd"] = SeoMetadata.ToJson(new object[]
            {
                new Dictionary<string, object?>
                {
                    ["@context"] = "https://schema.org",
                    ["@type"] = "LearningResource",
                    ["name"] = model.Name,
                    ["description"] = SeoMetadata.CleanDescription(model.Description, 500),
                    ["url"] = canonicalUrl,
                    ["inLanguage"] = culture,
                    ["learningResourceType"] = _text["seo.details.type"].Value,
                    ["timeRequired"] = model.DurationMinutes is > 0 ? $"PT{model.DurationMinutes}M" : null
                },
                new Dictionary<string, object?>
                {
                    ["@context"] = "https://schema.org",
                    ["@type"] = "BreadcrumbList",
                    ["itemListElement"] = isEnglish
                        ? new object[]
                        {
                            Breadcrumb(1, _text["Accueil"].Value, SeoMetadata.AbsoluteUrl(Request, "/en", _configuration)),
                            Breadcrumb(2, _text["Examens"].Value, SeoMetadata.AbsoluteUrl(Request, "/en/exams", _configuration) + "#exams-title"),
                            Breadcrumb(3, model.Name, canonicalUrl)
                        }
                        : new object[]
                        {
                            Breadcrumb(1, _text["Accueil"].Value, SeoMetadata.AbsoluteUrl(Request, "/fr", _configuration)),
                            Breadcrumb(2, _text["Examens"].Value, SeoMetadata.AbsoluteUrl(Request, "/fr", _configuration) + "#exams-title"),
                            Breadcrumb(3, model.Name, canonicalUrl)
                        }
                }
            });
        }
        return View(model);
    }

    private string RouteCulture()
    {
        var routeCulture = RouteData.Values["culture"]?.ToString();
        return routeCulture is "fr" or "en"
            ? routeCulture
            : LocalizedPublicRoutes.CultureFromPath(Request.Path.Value);
    }

    private static Dictionary<string, object?> Breadcrumb(int position, string name, string item) => new()
    {
        ["@type"] = "ListItem",
        ["position"] = position,
        ["name"] = name,
        ["item"] = item
    };

    [HttpGet("/{culture:culture}/examens/{slug}/commencer", Name = "PublicExamStart")]
    [HttpGet("/en/exams/{slug}/start", Name = "PublicExamStartEnglish")]
    [HttpGet("/examens/{slug}/commencer")]
    public async Task<IActionResult> Start(string slug, Guid? skillId, bool random, CancellationToken cancellationToken)
    {
        var exam = (await _catalog.GetDetailsAsync(slug, RouteCulture()))?.ToViewModel();
        if (exam == null)
            return NotFound();
        if (!exam.CanStart)
            return RedirectToRoute(LocalizedPublicRoutes.Details(RouteCulture()),
                LocalizedPublicRoutes.Values(RouteCulture(), ("slug", slug)));

        return View("Start", new StartExamAttemptRequest
        {
            ExamSlug = exam.SourceSlug,
            SkillId = skillId,
            Random = random
        });
    }

    [HttpGet("/Home/Details")]
    public IActionResult LegacyDetails(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        return RedirectToRoutePermanent(
            LocalizedPublicRoutes.Details("fr"),
            LocalizedPublicRoutes.Values("fr", ("slug", slug)));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartAttempt(StartExamAttemptRequest request, CancellationToken cancellationToken)
    {
        request.GuestEmail = request.GuestEmail?.Trim() ?? string.Empty;
        ModelState.Remove(nameof(request.GuestEmail));
        TryValidateModel(request);
        if (!ModelState.IsValid)
            return View("Start", request);

        var started = await _attemptStart.StartAsync(new Certiva.Application.Attempts.StartAttemptRequest(request.ExamSlug, request.SkillId, request.Random, request.GuestEmail, RouteCulture()), cancellationToken);
        if (started == null)
            return NotFound();

        Response.Cookies.Append(AttemptCookieName(started.AttemptId), started.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Strict,
            Secure = Request.IsHttps,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

        return RedirectToRoute(LocalizedPublicRoutes.Take(RouteCulture()),
            LocalizedPublicRoutes.Values(RouteCulture(), ("slug", request.ExamSlug), ("attemptId", started.AttemptId)));
    }

    [HttpGet("/{culture:culture}/examens/{slug}/passer/{attemptId:guid}", Name = "PublicExamTake")]
    [HttpGet("/en/exams/{slug}/take/{attemptId:guid}", Name = "PublicExamTakeEnglish")]
    [HttpGet("/examens/{slug}/passer/{attemptId:guid}")]
    public Task<IActionResult> Take(string slug, Guid attemptId, CancellationToken cancellationToken) =>
        LoadTakeAsync(attemptId, slug, cancellationToken);

    [HttpGet("/Home/Take")]
    public Task<IActionResult> LegacyTake(Guid attemptId, CancellationToken cancellationToken) =>
        LoadTakeAsync(attemptId, null, cancellationToken);

    private async Task<IActionResult> LoadTakeAsync(Guid attemptId, string? routeSlug, CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(AttemptCookieName(attemptId), out var accessToken);
        var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        var resumed = await _attemptReads.ResumeAsync(attemptId, userId, accessToken, cancellationToken);
        if (resumed.Status == Certiva.Application.Attempts.ResumeAttemptResultStatus.Expired)
            return StatusCode(StatusCodes.Status410Gone, _text["Cette tentative est expirée ou déjà terminée."]);
        if (resumed.Status != Certiva.Application.Attempts.ResumeAttemptResultStatus.Ready || resumed.Session == null)
            return NotFound();

        var resumedModel = resumed.Session.ToViewModel();

        if (string.IsNullOrWhiteSpace(routeSlug)
            || !string.Equals(routeSlug, resumedModel.ExamSlug, StringComparison.OrdinalIgnoreCase)
            || RouteCulture() != resumedModel.Culture)
        {
            return RedirectToRoutePermanent(LocalizedPublicRoutes.Take(resumedModel.Culture),
                LocalizedPublicRoutes.Values(resumedModel.Culture, ("slug", resumedModel.ExamSlug), ("attemptId", attemptId)));
        }

        return View(resumedModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAnswer([FromBody] SaveExamAnswerRequest request, CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(AttemptCookieName(request.AttemptId), out var accessToken);
        var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        var result = await _attemptAutosave.SaveAnswerAsync(new Certiva.Application.Attempts.SaveAnswerRequest(request.AttemptId, request.AttemptQuestionId, request.SelectedChoiceIds, request.TextAnswer), userId, accessToken, cancellationToken);
        return result.Status switch
        {
            Certiva.Application.Attempts.SaveAnswerResultStatus.Saved => Ok(new { savedAtUtc = result.SavedAtUtc }),
            Certiva.Application.Attempts.SaveAnswerResultStatus.NotFoundOrUnauthorized => NotFound(),
            Certiva.Application.Attempts.SaveAnswerResultStatus.AttemptClosed => StatusCode(StatusCodes.Status410Gone),
            _ => BadRequest(new { error = _text["La réponse envoyée ne correspond pas à cette question."].Value })
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitAttempt(Guid attemptId, CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(AttemptCookieName(attemptId), out var accessToken);
        var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        var result = await _attemptSubmit.SubmitAsync(attemptId, userId, accessToken, cancellationToken);
        if (result == null)
            return NotFound();
        return RedirectToRoute(LocalizedPublicRoutes.Result(result.Culture),
            LocalizedPublicRoutes.Values(result.Culture, ("slug", result.ExamSlug), ("attemptId", attemptId)));
    }

    [HttpGet("/{culture:culture}/examens/{slug}/resultat/{attemptId:guid}", Name = "PublicExamResult")]
    [HttpGet("/en/exams/{slug}/result/{attemptId:guid}", Name = "PublicExamResultEnglish")]
    [HttpGet("/examens/{slug}/resultat/{attemptId:guid}")]
    public Task<IActionResult> Result(string slug, Guid attemptId, CancellationToken cancellationToken) =>
        LoadResultAsync(attemptId, slug, cancellationToken);

    [HttpGet("/Home/Result")]
    public Task<IActionResult> LegacyResult(Guid attemptId, CancellationToken cancellationToken) =>
        LoadResultAsync(attemptId, null, cancellationToken);

    private async Task<IActionResult> LoadResultAsync(Guid attemptId, string? routeSlug, CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(AttemptCookieName(attemptId), out var accessToken);
        var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        var resultDto = await _attemptReads.GetResultAsync(attemptId, userId, accessToken, cancellationToken);
        var result = resultDto?.ToViewModel();
        if (result == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(routeSlug)
            || !string.Equals(routeSlug, result.ExamSlug, StringComparison.OrdinalIgnoreCase)
            || RouteCulture() != result.Culture)
        {
            return RedirectToRoutePermanent(LocalizedPublicRoutes.Result(result.Culture),
                LocalizedPublicRoutes.Values(result.Culture, ("slug", result.ExamSlug), ("attemptId", attemptId)));
        }

        ViewData["Title"] = _text["seo.result.title", result.ExamName].Value;
        ViewData["FullTitle"] = true;
        ViewData["MetaDescription"] = _text["seo.result.description", result.ExamName, result.Percentage, result.CorrectAnswers, result.TotalQuestions].Value;
        return View("Result", result);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static string AttemptCookieName(Guid attemptId) => $"ExamsApp.Attempt.{attemptId:N}";
}
