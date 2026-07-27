using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Entities;
using Portfolio.Services;
using Portfolio.Services.Metrik;
using Portfolio.SITE_UI.Models;

namespace Portfolio.SITE_UI.Controllers;

public class HomeController : Controller
{
    /// <summary>Seçilen dilin hatırlandığı çerez (prototipteki localStorage["st-lang"] karşılığı).</summary>
    private const string DilCerezi = "st-lang";

    private readonly IContentService _icerik;
    private readonly IMetrikKuyrugu? _metrik;

    /// <param name="metrik">
    /// Metrik kuyruğu. **İsteğe bağlı**: DB yokken (Mac'te geliştirme) kayıtlı
    /// olmaz ve site metriksiz çalışır — ölçüm, sitenin çalışmasının ön koşulu değil.
    /// </param>
    public HomeController(IContentService icerik, IMetrikKuyrugu? metrik = null)
    {
        _icerik = icerik;
        _metrik = metrik;
    }

    /// <summary>
    /// Sayfa görüntülemeyi SUNUCU tarafında kaydeder.
    /// <para>
    /// Neden istemcide değil: JS kapalı olsa da, reklam engelleyici çalışsa da
    /// sayılsın. Kendi sayfamıza gelen kendi isteğimizi sayıyoruz — engellenecek
    /// bir üçüncü taraf isteği yok.
    /// </para>
    /// <para>
    /// Kuyruğa atılıp geçilir: yanıt DB'yi beklemez (bkz. <see cref="MetrikKuyrugu"/>).
    /// </para>
    /// </summary>
    private void ZiyaretiKaydet(string dil)
    {
        if (_metrik is null) return;

        var tarayici = Request.Headers.UserAgent.ToString();
        if (IstekCozumleyici.BotMu(tarayici)) return;

        _metrik.Ekle(new ZiyaretIsi(
            DateTime.UtcNow,
            // Gerçek IP: Nginx `X-Real-IP`'yi Cloudflare'in CF-Connecting-IP'sinden
            // dolduruyor, UseForwardedHeaders bunu RemoteIpAddress'e yazıyor.
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            tarayici,
            Request.Path.Value ?? "/",
            dil,
            Request.Headers.Referer.ToString(),
            Request.Host.Host));
    }

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

        ZiyaretiKaydet(model.Lang);

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
