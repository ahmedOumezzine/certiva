using Certiva.Infrastructure.Data;
using Certiva.Infrastructure.Data.Seed;
using Certiva.Services.Exams;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
using Certiva.Localization;
using Certiva.Services.Seo;
using Certiva.Application.Exams;
using Certiva.Infrastructure.Data.Queries;
using Certiva.Infrastructure;
using System.IO.Compression;
using System.Xml.Linq;
using Certiva.Domain.Exams;
using Certiva.Domain.Attempts;
using Certiva.Domain.Enums;

var builder = WebApplication.CreateBuilder(args);

// Local developer settings are optional and intentionally excluded from Git.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddCertivaInfrastructure(connectionString);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Keep accounts already present in the Identity schema usable after the move.
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);
    options.Lockout.MaxFailedAccessAttempts = 3;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RouteOptions>(options =>
    options.ConstraintMap.Add("culture", typeof(SupportedCultureRouteConstraint)));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/fr/login";
    options.AccessDeniedPath = "/fr/login?accessDenied=true";
    options.Cookie.Name = "ExamsApp.Auth";
    options.Cookie.HttpOnly = true;
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        var culture = LocalizedPublicRoutes.CultureFromPath(context.Request.Path.Value);
        var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
        context.Response.Redirect(QueryHelpers.AddQueryString($"/{culture}/login", "returnUrl", returnUrl));
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        var culture = LocalizedPublicRoutes.CultureFromPath(context.Request.Path.Value);
        context.Response.Redirect($"/{culture}/login?accessDenied=true");
        return Task.CompletedTask;
    };
});

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
        options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)));
builder.Services.AddMemoryCache();
builder.Services.AddOutputCache();
builder.Services.AddScoped<Certiva.Application.Exams.IExamPublicationValidator, Certiva.Services.Exams.LocalizedExamPublicationValidator>();
builder.Services.AddScoped<Certiva.Application.Exams.IExamCatalogService, Certiva.Application.Exams.ExamCatalogService>();
builder.Services.AddScoped<Certiva.Application.Attempts.IExamAttemptReadService, Certiva.Application.Attempts.ExamAttemptReadService>();
builder.Services.AddScoped<Certiva.Application.Attempts.IExamAttemptAutosaveService, Certiva.Application.Attempts.ExamAttemptAutosaveService>();
builder.Services.AddScoped<Certiva.Application.Attempts.IExamAttemptStartService, Certiva.Application.Attempts.ExamAttemptStartService>();
builder.Services.AddSingleton<Certiva.Application.Security.IGuestAttemptTokenService, Certiva.Application.Security.GuestAttemptTokenService>();
builder.Services.AddScoped<Certiva.Application.Attempts.IExamAttemptSubmitService, Certiva.Application.Attempts.ExamAttemptSubmitService>();
builder.Services.AddScoped<Certiva.Areas.Admin.Services.IExamAdminService, Certiva.Areas.Admin.Services.ExamAdminService>();
builder.Services.AddScoped<Certiva.Areas.Admin.Services.ISkillAdminService, Certiva.Areas.Admin.Services.SkillAdminService>();
builder.Services.AddScoped<Certiva.Areas.Admin.Services.IQuestionAdminService, Certiva.Areas.Admin.Services.QuestionAdminService>();
builder.Services.AddScoped<Certiva.Areas.Admin.Services.IAdminAttemptService, Certiva.Areas.Admin.Services.AdminAttemptService>();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "text/css", "application/javascript", "image/svg+xml" });
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

var app = builder.Build();

await SeedAdminIdentityAsync(app.Services, app.Configuration);
if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("SeedData:ExamsDemo"))
    await SeedDemoExamsAsync(app.Services, app.Configuration.GetValue<bool>("SeedData:ResetDemoData"));

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://www.googletagmanager.com https://pagead2.googlesyndication.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net https://unpkg.com; " +
        "img-src 'self' https: data:; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "object-src 'none'; " +
        "base-uri 'self';";
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var headers = ctx.Context.Response.GetTypedHeaders();
        var path = ctx.File.PhysicalPath;

        if (!string.IsNullOrWhiteSpace(path) && (path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".woff", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase)))
        {
            headers.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromDays(365)
            };
            headers.Expires = DateTimeOffset.UtcNow.AddYears(1);
        }
    }
});

app.UseCors("AllowAll");
app.UseRouting();
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("fr-FR")
    .AddSupportedCultures("fr-FR", "en-US")
    .AddSupportedUICultures("fr-FR", "en-US");
localizationOptions.RequestCultureProviders =
[
    new RouteCultureRequestCultureProvider(),
    new CookieRequestCultureProvider()
];
app.UseRequestLocalization(localizationOptions);
app.Use(async (context, next) =>
{
    var routeCulture = context.GetRouteValue("culture")?.ToString();
    if (routeCulture is not ("fr" or "en"))
    {
        var pathCulture = context.Request.Path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        routeCulture = pathCulture is "fr" or "en" ? pathCulture : null;
    }
    var cultureName = routeCulture switch
    {
        "fr" => "fr-FR",
        "en" => "en-US",
        _ => null
    };
    if (cultureName != null)
    {
        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(cultureName)),
            new CookieOptions
            {
                IsEssential = true,
                HttpOnly = false,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
    }

    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.MapGet("/", () => Results.Redirect("/fr", permanent: true));

app.MapControllerRoute(
    name: "localized-admin-dashboard-root",
    pattern: "{culture:culture}/Admin",
    defaults: new { area = "Admin", controller = "Dashboard", action = "Index" });

app.MapControllerRoute(
    name: "localized-admin-dashboard",
    pattern: "{culture:culture}/Admin/Dashboard/{action=Index}/{id?}",
    defaults: new { area = "Admin", controller = "Dashboard" });

app.MapControllerRoute(
    name: "localized-admin-exams",
    pattern: "{culture:culture}/Admin/Exams/{action=Index}/{id?}",
    defaults: new { area = "Admin", controller = "Exam" });

app.MapControllerRoute(
    name: "localized-admin-skills",
    pattern: "{culture:culture}/Admin/Skills/{action=Index}/{id?}",
    defaults: new { area = "Admin", controller = "Skill" });

app.MapControllerRoute(
    name: "localized-admin-questions",
    pattern: "{culture:culture}/Admin/Questions/{action=Index}/{id?}",
    defaults: new { area = "Admin", controller = "Question" });

app.MapControllerRoute(
    name: "localized-admin-attempts",
    pattern: "{culture:culture}/Admin/Attempts/{action=Index}/{id?}",
    defaults: new { area = "Admin", controller = "Attempts" });

app.MapControllerRoute(
    name: "localized-areas",
    pattern: "{culture:culture}/{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "public-exam-catalog-fr",
    pattern: "fr/examens",
    defaults: new { controller = "Home", action = "Index" });

app.MapControllerRoute(
    name: "public-exam-catalog-en",
    pattern: "en/exams",
    defaults: new { controller = "Home", action = "Index" });

app.MapControllerRoute(
    name: "localized-default",
    pattern: "{culture:culture}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "admin-dashboard-root",
    pattern: "Admin",
    defaults: new { area = "Admin", controller = "Dashboard", action = "Index" });

app.MapControllerRoute(
    name: "admin-dashboard",
    pattern: "Admin/Dashboard/{action=Index}/{id?}",
    defaults: new { area = "Admin", controller = "Dashboard" });

app.MapControllerRoute(
    name: "admin-exams",
    pattern: "Admin/Exams/{action=Index}/{id?}",
    defaults: new { area = "Admin", controller = "Exam" });

app.MapControllerRoute(
    name: "admin-skills",
    pattern: "Admin/Skills/{action=Index}/{id?}",
    defaults: new { area = "Admin", controller = "Skill" });

app.MapControllerRoute(
    name: "admin-questions",
    pattern: "Admin/Questions/{action=Index}/{id?}",
    defaults: new { area = "Admin", controller = "Question" });

app.MapControllerRoute(
    name: "admin-attempts",
    pattern: "Admin/Attempts/{action=Index}/{id?}",
    defaults: new { area = "Admin", controller = "Attempts" });

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/robots.txt", (HttpContext context, IConfiguration configuration) =>
{
    var sitemapUrl = SeoMetadata.AbsoluteUrl(context.Request, "/sitemap.xml", configuration);
    var robots = string.Join('\n',
        "User-agent: *",
        "Allow: /",
        $"Sitemap: {sitemapUrl}");
    return Results.Text(robots, "text/plain; charset=utf-8");
});

app.MapGet("/sitemap.xml", async (
    HttpContext context,
    ApplicationDbContext db,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    var exams = await db.Set<Exam>()
        .AsNoTracking()
        .Where(exam => exam.Status == Status.Published && exam.Slug != null && exam.Slug != string.Empty)
        .OrderBy(exam => exam.Slug)
        .Select(exam => new
        {
            exam.Slug,
            EnglishSlug = exam.Translations!.Where(translation => translation.Culture == "en")
                .Select(translation => translation.Slug).FirstOrDefault(),
            HasCompleteEnglishCatalogContent =
                exam.Translations!.Any(translation => translation.Culture == "en" && translation.Name != null && translation.Name != string.Empty) &&
                exam.Translations!.Any(translation => translation.Culture == "en" && translation.Description != null && translation.Description != string.Empty) &&
                exam.Translations!.Any(translation => translation.Culture == "en" && translation.Slug != null && translation.Slug != string.Empty) &&
                exam.Skills!.All(skill =>
                    (skill.Name == null || skill.Name == string.Empty || skill.Translations!.Any(translation => translation.Culture == "en" && translation.Name != null && translation.Name != string.Empty)) &&
                    (skill.Description == null || skill.Description == string.Empty || skill.Translations!.Any(translation => translation.Culture == "en" && translation.Description != null && translation.Description != string.Empty))) &&
                exam.Questions!.Where(question => question.Status == Status.Published).All(question =>
                    (question.Description == null || question.Description == string.Empty ||
                     question.Translations!.Any(translation => translation.Culture == "en" && translation.Description != null && translation.Description != string.Empty)) &&
                    question.Choices!.All(choice => choice.ChoiceText == null || choice.ChoiceText == string.Empty ||
                        choice.Translations!.Any(translation => translation.Culture == "en" && translation.ChoiceText != null && translation.ChoiceText != string.Empty))),
            HasCompleteEnglishTranslation =
                exam.Translations!.Any(translation => translation.Culture == "en" && translation.Name != null && translation.Name != string.Empty) &&
                exam.Translations!.Any(translation => translation.Culture == "en" && translation.Description != null && translation.Description != string.Empty) &&
                exam.Translations!.Any(translation => translation.Culture == "en" && translation.Slug != null && translation.Slug != string.Empty) &&
                exam.Skills!.All(skill =>
                    (skill.Name == null || skill.Name == string.Empty || skill.Translations!.Any(translation => translation.Culture == "en" && translation.Name != null && translation.Name != string.Empty)) &&
                    (skill.Description == null || skill.Description == string.Empty || skill.Translations!.Any(translation => translation.Culture == "en" && translation.Description != null && translation.Description != string.Empty))) &&
                exam.Questions!.Where(question => question.Status == Status.Published).All(question =>
                    (question.Description == null || question.Description == string.Empty ||
                     question.Translations!.Any(translation => translation.Culture == "en" && translation.Description != null && translation.Description != string.Empty)) &&
                    question.Choices!.All(choice => choice.ChoiceText == null || choice.ChoiceText == string.Empty ||
                        choice.Translations!.Any(translation => translation.Culture == "en" && translation.ChoiceText != null && translation.ChoiceText != string.Empty))),
            LastModified = exam.LastModifiedOnUtc ?? exam.CreatedOnUtc
        })
        .ToListAsync(cancellationToken);

    XNamespace sitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
    var urls = new List<XElement>
    {
        SitemapEntry(sitemapNamespace, SeoMetadata.AbsoluteUrl(context.Request, "/fr", configuration)),
        SitemapEntry(sitemapNamespace, SeoMetadata.AbsoluteUrl(context.Request, "/fr/examens", configuration))
    };

    if (exams.All(exam => exam.HasCompleteEnglishCatalogContent))
    {
        urls.Add(SitemapEntry(sitemapNamespace, SeoMetadata.AbsoluteUrl(context.Request, "/en", configuration)));
        urls.Add(SitemapEntry(sitemapNamespace, SeoMetadata.AbsoluteUrl(context.Request, "/en/exams", configuration)));
    }

    urls.AddRange(exams.Select(exam => SitemapEntry(
        sitemapNamespace,
        SeoMetadata.AbsoluteUrl(context.Request, $"/fr/examens/{Uri.EscapeDataString(exam.Slug!)}", configuration),
        exam.LastModified)));

    urls.AddRange(exams
        .Where(exam => exam.HasCompleteEnglishTranslation && !string.IsNullOrWhiteSpace(exam.EnglishSlug))
        .Select(exam => SitemapEntry(
            sitemapNamespace,
            SeoMetadata.AbsoluteUrl(context.Request, $"/en/exams/{Uri.EscapeDataString(exam.EnglishSlug!)}", configuration),
            exam.LastModified)));

    var document = new XDocument(
        new XDeclaration("1.0", "UTF-8", "yes"),
        new XElement(sitemapNamespace + "urlset", urls));

    return Results.Text(document.ToString(SaveOptions.DisableFormatting), "application/xml; charset=utf-8");
});

app.UseResponseCompression();
app.Run();

static XElement SitemapEntry(XNamespace ns, string location, DateTime? lastModified = null)
{
    var entry = new XElement(ns + "url", new XElement(ns + "loc", location));
    if (lastModified.HasValue)
        entry.Add(new XElement(ns + "lastmod", lastModified.Value.ToUniversalTime().ToString("yyyy-MM-dd")));
    return entry;
}

static async Task SeedAdminIdentityAsync(IServiceProvider services, IConfiguration configuration)
{
    using var scope = services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roleExists = await roleManager.RoleExistsAsync("Admin");
    if (!roleExists)
    {
        var roleResult = await roleManager.CreateAsync(new IdentityRole("Admin"));
        if (!roleResult.Succeeded)
            throw new InvalidOperationException("Unable to create the Admin role: " + string.Join("; ", roleResult.Errors.Select(error => error.Description)));
    }

    var email = configuration["AdminUser:Email"];
    var password = configuration["AdminUser:Password"];
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        return;

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var admin = await userManager.FindByEmailAsync(email);
    if (admin == null)
    {
        admin = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FullName = "Administrator" };
        var createResult = await userManager.CreateAsync(admin, password);
        if (!createResult.Succeeded)
            throw new InvalidOperationException("Unable to create the configured admin account: " + string.Join("; ", createResult.Errors.Select(error => error.Description)));
    }

    if (!await userManager.IsInRoleAsync(admin, "Admin"))
    {
        var addRoleResult = await userManager.AddToRoleAsync(admin, "Admin");
        if (!addRoleResult.Succeeded)
            throw new InvalidOperationException("Unable to assign the Admin role: " + string.Join("; ", addRoleResult.Errors.Select(error => error.Description)));
    }
}

static async Task SeedDemoExamsAsync(IServiceProvider services, bool resetDemoData = false)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger(nameof(ExamDemoSeeder));
    await ExamDemoSeeder.SeedAsync(db, logger, resetDemoData);
}

public partial class Program { }
