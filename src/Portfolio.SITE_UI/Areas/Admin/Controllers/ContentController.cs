using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Entities;
using Portfolio.Services.Admin;

namespace Portfolio.SITE_UI.Areas.Admin.Controllers;

/// <summary>
/// İçerik düzenleme (Faz 5.2). Tüm yazma uçları auth + CSRF arkasında.
/// Bölüm tanımları tek kaynaktan gelir (<see cref="AdminSections"/>) → 8 ayrı
/// controller/view yerine tek jenerik akış.
/// </summary>
[Area("Admin")]
[Route("admin/icerik")]
[Authorize(Policy = "AdminPolicy")]
public class ContentController : Controller
{
    private readonly IAdminContentService _icerik;

    public ContentController(IAdminContentService icerik) => _icerik = icerik;

    [HttpGet("{slug?}")]
    public async Task<IActionResult> Bolum(string? slug, string? lang, CancellationToken ct)
    {
        slug ??= AdminSections.Hepsi[0].Slug;
        var model = await _icerik.BolumGetirAsync(slug, Lang.Normalize(lang), ct);
        return View("Bolum", model);
    }

    [HttpPost("{slug}/kaydet")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]      // birkaç kapak görseli + form alanları
    public async Task<IActionResult> Kaydet(string slug, string lang, CancellationToken ct)
    {
        var bolum = AdminSections.Bul(slug);
        if (bolum is null) return NotFound();
        lang = Lang.Normalize(lang);

        var form = Request.Form.ToDictionary(x => x.Key, x => (string?)x.Value.ToString());

        // 1) Dile bağlı metinler
        var kopyalar = bolum.Kopyalar
            .Where(k => form.ContainsKey($"kopya.{k.Anahtar}"))
            .ToDictionary(k => k.Anahtar, k => form[$"kopya.{k.Anahtar}"]);
        if (kopyalar.Count > 0) await _icerik.KopyaKaydetAsync(lang, kopyalar, ct);

        // 2) Liste alanları
        if (bolum.Liste != ListeTipi.Yok) await _icerik.ListeKaydetAsync(bolum.Liste, lang, form, ct);

        // 3) Oyun kapakları (varsa) — doğrulama başarısızsa o dosya atlanır, diğerleri kaydedilir
        var uyarilar = new List<string>();
        foreach (var dosya in Request.Form.Files.Where(f => f.Name.StartsWith("gorsel[", StringComparison.Ordinal)))
        {
            if (dosya.Length == 0) continue;
            var ham = dosya.Name.AsSpan()["gorsel[".Length..].TrimEnd(']');
            if (!int.TryParse(ham, out var ix)) continue;

            await using var akis = dosya.OpenReadStream();
            using var bellek = new MemoryStream();          // doğrulama için geri sarılabilir akış gerekiyor
            await akis.CopyToAsync(bellek, ct);
            bellek.Position = 0;

            var sonuc = await _icerik.OyunGorseliYukleAsync(ix, bellek, bellek.Length, ct);
            if (!sonuc.Gecerli) uyarilar.Add($"{ix + 1}. oyun: {sonuc.Hata}");
        }

        TempData["Toast"] = uyarilar.Count > 0 ? string.Join(" · ", uyarilar) : "Kaydedildi";
        return RedirectToAction(nameof(Bolum), new { slug, lang });
    }

    [HttpPost("{slug}/ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(string slug, string lang, CancellationToken ct)
    {
        var bolum = AdminSections.Bul(slug);
        if (bolum is null || bolum.Liste == ListeTipi.Yok) return NotFound();
        await _icerik.OgeEkleAsync(bolum.Liste, ct);
        TempData["Toast"] = "Kayıt eklendi";
        return RedirectToAction(nameof(Bolum), new { slug, lang });
    }

    [HttpPost("{slug}/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(string slug, int index, string lang, CancellationToken ct)
    {
        var bolum = AdminSections.Bul(slug);
        if (bolum is null || bolum.Liste == ListeTipi.Yok) return NotFound();
        await _icerik.OgeSilAsync(bolum.Liste, index, ct);
        TempData["Toast"] = "Kayıt silindi";
        return RedirectToAction(nameof(Bolum), new { slug, lang });
    }

    [HttpPost("{slug}/tasi")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Tasi(string slug, int index, int yon, string lang, CancellationToken ct)
    {
        var bolum = AdminSections.Bul(slug);
        if (bolum is null || bolum.Liste == ListeTipi.Yok) return NotFound();
        await _icerik.OgeTasiAsync(bolum.Liste, index, Math.Sign(yon), ct);
        return RedirectToAction(nameof(Bolum), new { slug, lang });
    }

    [HttpPost("{slug}/gorsel-kaldir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GorselKaldir(string slug, int index, string lang, CancellationToken ct)
    {
        await _icerik.OyunGorseliKaldirAsync(index, ct);
        TempData["Toast"] = "Görsel kaldırıldı";
        return RedirectToAction(nameof(Bolum), new { slug, lang });
    }

    [HttpPost("doldur")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BoslariDoldur(string slug, string lang, CancellationToken ct)
    {
        var sayi = await _icerik.BoslariTrdenDoldurAsync(lang, ct);
        TempData["Toast"] = sayi > 0 ? $"{sayi} alan TR'den dolduruldu" : "Doldurulacak boş alan yok";
        return RedirectToAction(nameof(Bolum), new { slug, lang });
    }

    // ---- JSON içe/dışa aktarma (DB'ye geçilse de yedek olarak kalır) ----

    [HttpGet("disa-aktar")]
    public async Task<IActionResult> DisaAktar(CancellationToken ct)
    {
        var json = await _icerik.DisaAktarAsync(ct);
        var ad = $"portfolyo-icerik-{DateTime.Now:yyyy-MM-dd-HHmm}.json";
        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", ad);
    }

    [HttpPost("ice-aktar")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5 * 1024 * 1024)]              // 5 MB: içerik JSON'u için fazlasıyla yeter
    public async Task<IActionResult> IceAktar(IFormFile? dosya, string? slug, CancellationToken ct)
    {
        if (dosya is null || dosya.Length == 0)
        {
            TempData["Toast"] = "Dosya seçilmedi";
            return RedirectToAction(nameof(Bolum), new { slug });
        }
        try
        {
            using var okuyucu = new StreamReader(dosya.OpenReadStream());
            await _icerik.IceAktarAsync(await okuyucu.ReadToEndAsync(ct), ct);
            TempData["Toast"] = "JSON yüklendi";
        }
        catch (Exception ex)
        {
            TempData["Toast"] = $"Yükleme başarısız: {ex.Message}";
        }
        return RedirectToAction(nameof(Bolum), new { slug });
    }

    /// <summary>
    /// Demo önizleme çerçevesi. Kaydedilmiş HTML'i döner; iframe `sandbox="allow-scripts"`
    /// ile gömülür → allow-same-origin YOK.
    /// </summary>
    [HttpGet("demo-onizle/{index:int}")]
    public async Task<IActionResult> DemoOnizle(int index, CancellationToken ct)
    {
        var html = await _icerik.DemoHtmlAsync(index, ct);
        return Content(html, "text/html; charset=utf-8");
    }
}
