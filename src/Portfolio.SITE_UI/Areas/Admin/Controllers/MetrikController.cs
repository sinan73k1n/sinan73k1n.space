using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Repositories.Sql;

namespace Portfolio.SITE_UI.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/metrikler")]
[Authorize(Policy = "AdminPolicy")]
public class MetrikController : Controller
{
    /// <summary>
    /// Metrik okuyucu **isteğe bağlı**: bağlantı dizesi yokken (Mac'te geliştirme)
    /// kayıtlı değildir. O durumda ekran "metrik yalnız DB varken toplanır" der,
    /// hata vermez.
    /// </summary>
    private readonly IMetrikOkuyucu? _okuyucu;

    public MetrikController(IMetrikOkuyucu? okuyucu = null) => _okuyucu = okuyucu;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? donem, CancellationToken ct)
    {
        var secilen = Donem.Coz(donem);
        ViewData["Donem"] = secilen.Anahtar;
        ViewData["DonemAdi"] = secilen.Ad;

        if (_okuyucu is null) return View(model: null);

        var bugun = DateOnly.FromDateTime(DateTime.UtcNow);
        var ozet = await _okuyucu.OzetAsync(bugun.AddDays(-(secilen.Gun - 1)), bugun, ct);

        return View(ozet);
    }
}

/// <summary>Ekrandaki dönem seçenekleri. Gün sayısı bugünü DE kapsar.</summary>
public sealed record Donem(string Anahtar, string Ad, int Gun)
{
    public static readonly Donem[] Hepsi =
    {
        new("gun",  "Bugün",       1),
        new("hafta","Son 7 gün",   7),
        new("ay",   "Son 30 gün", 30),
        new("yil",  "Son 365 gün", 365)
    };

    /// <summary>Bilinmeyen/eksik değer → hafta (en çok işe yarayan varsayılan).</summary>
    public static Donem Coz(string? anahtar) =>
        Hepsi.FirstOrDefault(x => x.Anahtar == anahtar) ?? Hepsi[1];
}
