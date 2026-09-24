using Certiva.Infrastructure.Data;
using Certiva.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Certiva.Localization;

namespace Certiva.Controllers;

public sealed class AccountController : Controller
{
    private readonly IStringLocalizer<SharedResource> _text;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IStringLocalizer<SharedResource> text)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _text = text;
    }

    [HttpGet("/{culture:culture}/login", Name = "AdminLogin")]
    [HttpGet("/login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl, bool accessDenied = false)
    {
        if (string.Equals(Request.Path.Value, "/login", StringComparison.OrdinalIgnoreCase))
        {
            var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en"
                ? "en"
                : "fr";
            var target = $"/{culture}/login";
            var query = new Dictionary<string, string?>();
            if (!string.IsNullOrWhiteSpace(returnUrl))
                query["returnUrl"] = returnUrl;
            if (accessDenied)
                query["accessDenied"] = "true";
            return RedirectPermanent(query.Count == 0
                ? target
                : Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(target, query));
        }

        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            return LocalRedirect(IsAdminReturnUrl(returnUrl) ? returnUrl! : LocalizedAdminHome());

        var model = new AdminLoginViewModel { ReturnUrl = returnUrl };
        if (accessDenied)
            ModelState.AddModelError(string.Empty, _text["Email ou mot de passe invalide, ou compte non autorisé."]);

        return View(model);
    }

    [HttpPost("/{culture:culture}/login")]
    [HttpPost("/login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model)
    {
        model.Email = model.Email?.Trim() ?? string.Empty;
        ModelState.Remove(nameof(model.Email));
        TryValidateModel(model);
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, _text["Email ou mot de passe invalide, ou compte non autorisé."]);
            return View(model);
        }

        var passwordResult = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
        if (!passwordResult.Succeeded || !await _userManager.IsInRoleAsync(user, "Admin"))
        {
            ModelState.AddModelError(string.Empty, _text["Email ou mot de passe invalide, ou compte non autorisé."]);
            return View(model);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(IsAdminReturnUrl(model.ReturnUrl) ? model.ReturnUrl! : LocalizedAdminHome());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("/{culture:culture}/logout")]
    [HttpPost("/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        var culture = LocalizedPublicRoutes.CultureFromPath(Request.Path.Value);
        return Redirect($"/{culture}");
    }

    private bool IsAdminReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
            return false;

        var queryIndex = returnUrl.IndexOfAny(['?', '#']);
        var path = queryIndex >= 0 ? returnUrl[..queryIndex] : returnUrl;
        if (path.StartsWith("/fr/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/en/", StringComparison.OrdinalIgnoreCase))
        {
            path = path[3..];
        }
        else if (path.Equals("/fr", StringComparison.OrdinalIgnoreCase) ||
                 path.Equals("/en", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return path.Equals("/Admin", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/Admin/", StringComparison.OrdinalIgnoreCase);
    }

    private string LocalizedAdminHome() =>
        $"/{(RouteData.Values["culture"]?.ToString() == "en" ? "en" : "fr")}/Admin";
}
