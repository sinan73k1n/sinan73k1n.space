using Portfolio.Entities.Metrik;
using Portfolio.Repositories.Sql;
using Portfolio.Services.Metrik;

namespace Portfolio.SITE_UI.Metrik;

/// <summary>
/// Kuyruktaki metrik işlerini DB'ye yazan arka plan işçisi.
///
/// <para>
/// Tek tüketici (kanal <c>SingleReader</c>): günlük tuz aramaları ve yazımlar
/// sıraya girer, gün dönümündeki yarış tek noktada toplanır.
/// </para>
/// <para>
/// ⚠️ Bu döngü <b>asla ölmemeli</b>: metrik yazımındaki bir hata siteyi etkilemez
/// ama işçi ölürse kuyruk dolar ve tüm metrikler sessizce kaybolur. O yüzden her
/// iş kendi try/catch'inde.
/// </para>
/// </summary>
public sealed class MetrikYazici : BackgroundService
{
    private readonly IMetrikKuyrugu _kuyruk;
    private readonly IMetrikDeposu _depo;
    private readonly ILogger<MetrikYazici> _gunluk;

    public MetrikYazici(IMetrikKuyrugu kuyruk, IMetrikDeposu depo, ILogger<MetrikYazici> gunluk)
    {
        _kuyruk = kuyruk; _depo = depo; _gunluk = gunluk;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var is_ in _kuyruk.OkuAsync(ct))
        {
            try
            {
                switch (is_)
                {
                    case ZiyaretIsi z: await ZiyaretiYazAsync(z, ct); break;
                    case OlayIsi o: await OlaylariYazAsync(o, ct); break;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;   // uygulama kapanıyor
            }
            catch (Exception ex)
            {
                // Yut ve devam et — metrik kaybı sitenin çalışmasından önemsizdir.
                _gunluk.LogWarning(ex, "Metrik yazılamadı ({Tip})", is_.GetType().Name);
            }
        }
    }

    private async Task<string> KimlikAsync(DateOnly gun, string? ip, string? tarayici, CancellationToken ct)
    {
        var tuz = await _depo.GunlukTuzAsync(gun, ZiyaretciKimligi.TuzUret, ct);
        return ZiyaretciKimligi.Hesapla(ip, tarayici, tuz);
    }

    private async Task ZiyaretiYazAsync(ZiyaretIsi z, CancellationToken ct)
    {
        var gun = DateOnly.FromDateTime(z.ZamanUtc);
        var (tip, host) = IstekCozumleyici.KaynagiCoz(z.Referrer, z.KendiHost);

        await _depo.ZiyaretYazAsync(new ZiyaretRow
        {
            ZamanUtc = z.ZamanUtc,
            Gun = gun,
            ZiyaretciHash = await KimlikAsync(gun, z.Ip, z.Tarayici, ct),
            Yol = Kirp(z.Yol, 200),
            Dil = z.Dil,
            KaynakTipi = tip,
            KaynakHost = Kirp(host, 120),
            Cihaz = IstekCozumleyici.CihazCoz(z.Tarayici)
        }, ct);
    }

    private async Task OlaylariYazAsync(OlayIsi o, CancellationToken ct)
    {
        var gun = DateOnly.FromDateTime(o.ZamanUtc);
        var kimlik = await KimlikAsync(gun, o.Ip, o.Tarayici, ct);

        var satirlar = o.Olaylar.Select(x => new OlayRow
        {
            ZamanUtc = o.ZamanUtc,
            Gun = gun,
            ZiyaretciHash = kimlik,
            Tip = x.Tip,
            Deger = Kirp(x.Deger, 200),
            SaniyeSure = x.SaniyeSure
        }).ToList();

        await _depo.OlaylarYazAsync(satirlar, ct);
    }

    /// <summary>Kolon sınırını aşan değer yazma hatası vermesin diye kırpılır.</summary>
    private static string Kirp(string? deger, int uzunluk) =>
        string.IsNullOrEmpty(deger) ? "" : deger.Length <= uzunluk ? deger : deger[..uzunluk];
}
