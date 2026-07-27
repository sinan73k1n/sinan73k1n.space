using Microsoft.AspNetCore.Mvc;
using Portfolio.Entities.Metrik;
using Portfolio.Services.Metrik;

namespace Portfolio.SITE_UI.Controllers;

/// <summary>
/// Sayfa içi olayları toplayan uç. İstemci <c>navigator.sendBeacon</c> ile gönderir.
///
/// <para>
/// ⚠️ <b>Bu uç kimlik doğrulaması OLMADAN herkese açık</b> — olmak zorunda, ziyaretçi
/// gönderiyor. O yüzden gelen her şey düşman kabul edilir:
/// </para>
/// <list type="bullet">
///   <item>Gövde boyutu sınırlı (8 KB) — büyük gövdeyle bellek doldurulamasın.</item>
///   <item>Olay sayısı sınırlı (tek istekte en fazla 20).</item>
///   <item>Tip ve bölüm anahtarı <b>beyaz listeye</b> göre doğrulanır; tanınmayan atılır.</item>
///   <item>Süre üst sınıra kırpılır — "1 milyon saniye" yazıp ortalamayı bozamasın.</item>
///   <item>Bot imzalı istekler hiç kuyruğa girmez.</item>
///   <item>Yazma kuyruğa atılır: uç, DB'yi bekletmeden hemen 204 döner.</item>
/// </list>
/// <para>
/// Adres bilerek nötr (<c>/olcum</c>): "analytics", "track", "collect" gibi yollar
/// reklam engelleyici listelerinde doğrudan geçiyor.
/// </para>
/// </summary>
public class OlcumController : Controller
{
    /// <summary>Tek istekte kabul edilen en fazla olay.</summary>
    private const int AzamiOlay = 20;

    /// <summary>Bir bölümde geçirilebilecek makul en uzun süre (30 dk).</summary>
    private const int AzamiSaniye = 1800;

    private readonly IMetrikKuyrugu _kuyruk;

    public OlcumController(IMetrikKuyrugu kuyruk) => _kuyruk = kuyruk;

    public sealed class OlayGirdi
    {
        public string? Tip { get; set; }
        public string? Deger { get; set; }
        public int Sure { get; set; }
    }

    [HttpPost("/olcum")]
    [IgnoreAntiforgeryToken]   // sendBeacon token taşıyamaz; uç zaten yalnız sayaç artırıyor
    [RequestSizeLimit(8 * 1024)]
    public IActionResult Topla([FromBody] List<OlayGirdi>? girdiler)
    {
        if (girdiler is null || girdiler.Count == 0) return NoContent();

        var tarayici = Request.Headers.UserAgent.ToString();
        if (IstekCozumleyici.BotMu(tarayici)) return NoContent();

        var temiz = new List<(string, string, int)>();

        foreach (var g in girdiler.Take(AzamiOlay))
        {
            var tip = g.Tip?.Trim().ToLowerInvariant();
            if (tip is null || Array.IndexOf(OlayTipi.Hepsi, tip) < 0) continue;

            var deger = g.Deger?.Trim() ?? "";

            // Bölüm ve süre olaylarının değeri BİLİNEN bir bölüm olmak zorunda.
            // Serbest metin kabul etseydik tabloya sınırsız çeşitlilikte çöp girerdi.
            if ((tip == OlayTipi.Bolum || tip == OlayTipi.Sure) && !Bolumler.Gecerli(deger)) continue;

            if (deger.Length == 0) continue;

            var sure = tip == OlayTipi.Sure ? Math.Clamp(g.Sure, 1, AzamiSaniye) : 0;
            temiz.Add((tip, deger, sure));
        }

        if (temiz.Count > 0)
        {
            _kuyruk.Ekle(new OlayIsi(
                DateTime.UtcNow,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                tarayici,
                temiz));
        }

        // 204: gövde yok, tarayıcı bir şey beklemesin. sendBeacon zaten cevabı umursamaz.
        return NoContent();
    }
}
