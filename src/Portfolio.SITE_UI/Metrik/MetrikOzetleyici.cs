using Portfolio.Repositories.Sql;

namespace Portfolio.SITE_UI.Metrik;

/// <summary>
/// Gecelik bakım: dünün ham verisini kalıcı özete çevirir, 90 günden eski
/// ham veriyi ve tuzları siler.
///
/// <para>
/// <b>Neden cron değil de uygulama içi:</b> iş DB şemasını ve iş kurallarını
/// biliyor; ayrı bir script olsaydı özetleme mantığı iki yerde yaşardı.
/// Ayrıca deploy'la birlikte taşınıyor, ayrıca kurulum gerektirmiyor.
/// (`portfoliodb` YEDEĞİ ayrı bir iş ve o cron'da — orası şemadan bağımsız.)
/// </para>
/// <para>
/// <b>Açılışta da bir kez çalışır:</b> uygulama gece boyunca kapalı kaldıysa
/// (deploy, yeniden başlatma, sunucu kapanması) atlanan günler ilk açılışta
/// telafi edilir. <c>OzetlenecekGunlerAsync</c> "ham verisi var ama özeti yok"
/// diye baktığı için kaç gün atlandığı fark etmez.
/// </para>
/// </summary>
public sealed class MetrikOzetleyici : BackgroundService
{
    /// <summary>Ham veri saklama süresi (karar: Sinan, 2026-07-27).</summary>
    private const int HamSaklamaGun = 90;

    private readonly IServiceProvider _saglayici;
    private readonly ILogger<MetrikOzetleyici> _gunluk;

    public MetrikOzetleyici(IServiceProvider saglayici, ILogger<MetrikOzetleyici> gunluk)
    {
        _saglayici = saglayici; _gunluk = gunluk;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Açılışta hemen değil: uygulama daha yeni ayağa kalkıyor, migration ve
        // ilk istekler öncelikli. Bir dakika sonra başla.
        try { await Task.Delay(TimeSpan.FromMinutes(1), ct); }
        catch (OperationCanceledException) { return; }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await BakimYapAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Metrik bakımı başarısız olsa da site çalışmaya devam eder.
                _gunluk.LogWarning(ex, "Metrik özetleme başarısız — sonraki turda tekrar denenecek.");
            }

            try { await Task.Delay(SonrakiTuraKalan(), ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task BakimYapAsync(CancellationToken ct)
    {
        using var kapsam = _saglayici.CreateScope();
        var depo = kapsam.ServiceProvider.GetRequiredService<IMetrikDeposu>();

        var bugun = DateOnly.FromDateTime(DateTime.UtcNow);

        var gunler = await depo.OzetlenecekGunlerAsync(bugun, ct);
        foreach (var gun in gunler)
        {
            await depo.OzetleAsync(gun, ct);
            _gunluk.LogInformation("Metrik özetlendi: {Gun}", gun);
        }

        var silinen = await depo.TemizleAsync(bugun.AddDays(-HamSaklamaGun), ct);
        if (silinen > 0)
            _gunluk.LogInformation("Eski ham metrik silindi: {Adet} satır ({Gun} öncesi)", silinen, bugun.AddDays(-HamSaklamaGun));
    }

    /// <summary>
    /// Bir sonraki gece 03:15 UTC'ye kalan süre.
    /// <para>
    /// 03:15 seçildi: yedek işi 03:30'da çalışıyor — özet ondan ÖNCE bitsin ki
    /// alınan yedek o günün özetini de içersin.
    /// </para>
    /// </summary>
    private static TimeSpan SonrakiTuraKalan()
    {
        var simdi = DateTime.UtcNow;
        var hedef = simdi.Date.AddHours(3).AddMinutes(15);
        if (hedef <= simdi) hedef = hedef.AddDays(1);
        return hedef - simdi;
    }
}
