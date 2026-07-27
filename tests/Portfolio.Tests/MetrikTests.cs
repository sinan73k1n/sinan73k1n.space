using Portfolio.Entities.Metrik;
using Portfolio.Services.Metrik;

namespace Portfolio.Tests;

/// <summary>
/// Metrik toplamanın kural katmanı. DB gerektirmeyen, saf mantık testleri —
/// yanlış sınıflandırma sessizce yanlış rapora dönüşür, o yüzden kapıya alınıyor.
/// </summary>
public class MetrikTests
{
    // --- Ziyaretçi kimliği ---------------------------------------------------

    [Fact]
    public void Ayni_ziyaretci_ayni_gun_AYNI_kimlige_duser()
    {
        var tuz = ZiyaretciKimligi.TuzUret();

        var a = ZiyaretciKimligi.Hesapla("1.2.3.4", "Firefox", tuz);
        var b = ZiyaretciKimligi.Hesapla("1.2.3.4", "Firefox", tuz);

        Assert.Equal(a, b);   // yoksa tekil ziyaretçi sayısı şişerdi
    }

    [Fact]
    public void Tuz_degisince_AYNI_ziyaretci_BASKA_kimlige_duser()
    {
        // Günler arası takibin imkânsızlığı bu davranışa dayanıyor.
        var a = ZiyaretciKimligi.Hesapla("1.2.3.4", "Firefox", ZiyaretciKimligi.TuzUret());
        var b = ZiyaretciKimligi.Hesapla("1.2.3.4", "Firefox", ZiyaretciKimligi.TuzUret());

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Farkli_ziyaretciler_ayrisir_ve_ayrac_birlesmeyi_engeller()
    {
        var tuz = ZiyaretciKimligi.TuzUret();

        Assert.NotEqual(
            ZiyaretciKimligi.Hesapla("1.2.3.4", "Firefox", tuz),
            ZiyaretciKimligi.Hesapla("1.2.3.5", "Firefox", tuz));

        // Ayraç olmasaydı "1.2.3.4"+"5x" ile "1.2.3.45"+"x" aynı girdiye düşerdi.
        Assert.NotEqual(
            ZiyaretciKimligi.Hesapla("1.2.3.4", "5x", tuz),
            ZiyaretciKimligi.Hesapla("1.2.3.45", "x", tuz));
    }

    [Fact]
    public void Kimlik_ham_IP_icermez()
    {
        var kimlik = ZiyaretciKimligi.Hesapla("192.168.1.77", "Chrome", ZiyaretciKimligi.TuzUret());

        Assert.DoesNotContain("192.168", kimlik);
        Assert.Equal(32, kimlik.Length);   // 16 bayt hex
    }

    // --- Kaynak sınıflandırma ------------------------------------------------

    [Theory]
    [InlineData("https://www.google.com/search?q=x", KaynakTipi.Organik)]
    [InlineData("https://duckduckgo.com/", KaynakTipi.Organik)]
    [InlineData("https://yandex.com.tr/", KaynakTipi.Organik)]
    [InlineData("https://www.linkedin.com/feed/", KaynakTipi.Sosyal)]
    [InlineData("https://t.co/abc", KaynakTipi.Sosyal)]
    [InlineData("https://haber.example.com/yazi", KaynakTipi.Yonlendirme)]
    [InlineData("", KaynakTipi.Dogrudan)]
    [InlineData(null, KaynakTipi.Dogrudan)]
    [InlineData("bozuk-url", KaynakTipi.Dogrudan)]
    public void Kaynak_dogru_siniflanir(string? referrer, string beklenen)
    {
        var (tip, _) = IstekCozumleyici.KaynagiCoz(referrer, "sinan73k1n.space");
        Assert.Equal(beklenen, tip);
    }

    [Fact]
    public void Kendi_sitesinden_gelen_referrer_DOGRUDAN_sayilir()
    {
        // Site tek sayfa; iç gezinme yeni bir ziyaret kaynağı değildir.
        // Aksi hâlde "yonlendirme" kutusu kendi trafiğimizle dolardı.
        var (tip, host) = IstekCozumleyici.KaynagiCoz("https://sinan73k1n.space/?lang=en", "sinan73k1n.space");
        Assert.Equal(KaynakTipi.Dogrudan, tip);
        Assert.Equal("", host);

        // Alt alan adı da kendimiz sayılır (demo origin).
        var (tip2, _) = IstekCozumleyici.KaynagiCoz("https://demo.sinan73k1n.space/d/0", "sinan73k1n.space");
        Assert.Equal(KaynakTipi.Dogrudan, tip2);
    }

    [Fact]
    public void Kaynak_host_www_siz_ve_kucuk_harf_doner()
    {
        var (_, host) = IstekCozumleyici.KaynagiCoz("https://WWW.Example.COM/yol", "sinan73k1n.space");
        Assert.Equal("example.com", host);
    }

    // --- Bot elemesi ---------------------------------------------------------

    [Theory]
    [InlineData("Mozilla/5.0 (compatible; Googlebot/2.1)")]
    [InlineData("curl/8.4.0")]
    [InlineData("python-requests/2.31")]
    [InlineData("Mozilla/5.0 HeadlessChrome/120")]
    [InlineData("")]        // UA yok → gerçek tarayıcı değil
    [InlineData(null)]
    public void Botlar_elenir(string? tarayici) => Assert.True(IstekCozumleyici.BotMu(tarayici));

    [Theory]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120 Safari/537.36")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 Version/17.0 Mobile/15E148 Safari/604.1")]
    public void Gercek_tarayicilar_elenmez(string tarayici) => Assert.False(IstekCozumleyici.BotMu(tarayici));

    // --- Cihaz ---------------------------------------------------------------

    [Theory]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) Mobile/15E148", CihazTipi.Mobil)]
    [InlineData("Mozilla/5.0 (Linux; Android 13; Pixel 7) Mobile Safari/537.36", CihazTipi.Mobil)]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X) Mobile/15E148", CihazTipi.Tablet)]
    [InlineData("Mozilla/5.0 (Linux; Android 13; SM-X200) Safari/537.36", CihazTipi.Tablet)]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) Chrome/120", CihazTipi.Masaustu)]
    public void Cihaz_dogru_siniflanir(string tarayici, string beklenen) =>
        Assert.Equal(beklenen, IstekCozumleyici.CihazCoz(tarayici));

    // --- Bölüm doğrulama (beacon ucu herkese açık) ---------------------------

    [Fact]
    public void Bolum_anahtarlari_sayfadaki_id_lerle_ayni()
    {
        // Bu test bilerek "sabit değerleri tekrar yazıyor": biri Bolumler'i
        // değiştirirse Index.cshtml'deki data-bolum ile bağı kopar ve metrik
        // sessizce boşalır. Test o anda kırılsın.
        Assert.Equal(new[] { "top", "about", "stack", "games", "demos", "github", "contact" }, Bolumler.Sirali);
    }

    [Theory]
    [InlineData("github", true)]
    [InlineData("GITHUB", false)]           // büyük harf kabul edilmez
    [InlineData("uydurma", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Bilinmeyen_bolum_reddedilir(string? deger, bool beklenen) =>
        Assert.Equal(beklenen, Bolumler.Gecerli(deger));

    // --- Kuyruk --------------------------------------------------------------

    [Fact]
    public void Kuyruk_dolunca_BLOKLAMAZ_dusurur()
    {
        // Bu davranış kasıtlı: metrik kaybı tolere edilebilir, istek gecikmesi edilemez.
        var kuyruk = new MetrikKuyrugu(kapasite: 2);
        var is_ = new OlayIsi(DateTime.UtcNow, "1.2.3.4", "Chrome", Array.Empty<(string, string, int)>());

        Assert.True(kuyruk.Ekle(is_));
        Assert.True(kuyruk.Ekle(is_));
        Assert.False(kuyruk.Ekle(is_));      // kapasite doldu → düştü, beklemedi

        Assert.Equal(1, kuyruk.DusenSayisi);
    }
}
