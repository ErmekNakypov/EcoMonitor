using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EcoMonitor.Web.Controllers;

[AllowAnonymous]
public class LocalizationController : Controller
{
    private readonly IOptions<RequestLocalizationOptions> _options;

    public LocalizationController(IOptions<RequestLocalizationOptions> options)
    {
        _options = options;
    }

    // GET /Localization/SetCulture?culture=en-US&returnUrl=/Home/Index
    //
    // Writes the ASP.NET Core culture cookie in the format
    // CookieRequestCultureProvider expects ("c=en-US|uic=en-US") so the
    // RequestLocalizationMiddleware picks it up on the next request, then
    // local-redirects back to returnUrl. GET (not POST) is intentional: a
    // UI-language preference is not a security-sensitive write — the
    // attacker can already influence Accept-Language — so the cost of
    // forms + antiforgery outweighs the benefit, and a plain anchor link
    // keeps the dropdown working with JS disabled.
    [HttpGet]
    public IActionResult SetCulture(string culture, string? returnUrl)
    {
        // Reject anything not in the configured supported set. Writing an
        // unsupported culture to the cookie would cause every subsequent
        // request to fall through to the default (ru-RU) and look broken.
        if (string.IsNullOrWhiteSpace(culture)
            || _options.Value.SupportedUICultures is null
            || !_options.Value.SupportedUICultures.Any(c =>
                string.Equals(c.Name, culture, StringComparison.OrdinalIgnoreCase)))
        {
            return RedirectIfLocal(returnUrl);
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(new CultureInfo(culture))),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true   // exempt from the EU cookie-consent gate
            });

        return RedirectIfLocal(returnUrl);
    }

    private IActionResult RedirectIfLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }
}
