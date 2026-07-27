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
        // Dil seçimi — öncelik sırası (karar: 2026-07-27):
        //   1) Adres çubuğundaki `?lang=` (yalnız GEÇERLİ bir kod ise)
        //   2) Ziyaretçinin daha önce seçtiği dil (çerez)
        //   3) İlk ziyaret → Lang.Initial (EN)
        //
        // ⚠️ Burada Lang.Normalize KULLANILMAZ: o, bilinmeyen girdiyi çeviri
        // tabanına (TR) düşürür. Ziyaretçiye açılacak dil ayrı bir karardır —
        // geçersiz `?lang=zz` yazan birine TR değil, kendi çerezi ya da EN gelmeli.
        var istenen = Lang.Gecerli(lang) ? lang!.Trim().ToLowerInvariant() : null;
        var cerezdeki = Lang.Gecerli(Request.Cookies[DilCerezi])
            ? Request.Cookies[DilCerezi]!.Trim().ToLowerInvariant()
            : null;

        var secilen = istenen ?? cerezdeki ?? Lang.Initial;

        // Tercih yalnız kullanıcı AÇIKÇA seçtiğinde yazılır; ilk ziyarette
        // (EN varsayılanı) çerez oluşturulmaz — seçim değil, varsayılandır.
        if (istenen is not null)
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

    /// <summary>
    /// 404 ve diğer istemci hataları (Faz 7). `UseStatusCodePagesWithReExecute`
    /// buraya yönlendirir; **durum kodu korunur** — arama motoru 404'ü 200 sanmaz.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Bulunamadi(int? kod)
    {
        var durum = kod is >= 400 and < 600 ? kod.Value : 404;
        ViewData["DurumKodu"] = durum;
        Response.StatusCode = durum;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
