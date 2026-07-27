using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Entities;
using Portfolio.Services;
using Portfolio.SITE_UI.Models;

namespace Portfolio.SITE_UI.Controllers;

public class HomeController : Controller
{
    /// <summary>Seçilen dilin hatırlandığı çerez (prototipteki localStorage["st-lang"] karşılığı).</summary>
    private const string DilCerezi = "st-lang";

    private readonly IContentService _icerik;

    public HomeController(IContentService icerik) => _icerik = icerik;

    public async Task<IActionResult> Index(string? lang, CancellationToken ct)
    {
        // Öncelik: adres çubuğundaki ?lang= → çerez → varsayılan (TR).
        // Serbest girdi Lang.Normalize'dan geçer; bilinmeyen değer TR'ye düşer.
        var secilen = lang is not null
            ? Lang.Normalize(lang)
            : Lang.Normalize(Request.Cookies[DilCerezi]);

        if (lang is not null)
        {
            Response.Cookies.Append(DilCerezi, secilen, new CookieOptions
            {
                MaxAge = TimeSpan.FromDays(365),
                HttpOnly = false,        // ileride istemci tarafı da okuyabilsin
                IsEssential = true,      // işlevsel çerez (dil tercihi)
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });
        }

        var model = await _icerik.GetAsync(secilen, ct);

        ViewData["Lang"] = model.Lang;
        ViewData["Title"] = model.Meta.Name;
        ViewData["Description"] = model.T("heroLead");

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
