namespace Portfolio.Entities.Metrik;

/// <summary>
/// Ziyaretin nereden geldiği. Ham referrer saklanmaz — sınıflandırılmış hâli
/// yeter ve daha az kişisel veri demektir.
/// </summary>
public static class KaynakTipi
{
    /// <summary>Arama motoru (google, bing, yandex, duckduckgo…).</summary>
    public const string Organik = "organik";

    /// <summary>Referrer yok: adres çubuğuna yazılmış, yer imi, e-posta/uygulama içi.</summary>
    public const string Dogrudan = "dogrudan";

    /// <summary>Sosyal ağ (linkedin, x, reddit, youtube…).</summary>
    public const string Sosyal = "sosyal";

    /// <summary>Başka bir site.</summary>
    public const string Yonlendirme = "yonlendirme";

    public static readonly string[] Hepsi = { Organik, Dogrudan, Sosyal, Yonlendirme };
}

/// <summary>Sayfa içi olay tipleri (istemciden beacon ile gelir).</summary>
public static class OlayTipi
{
    /// <summary>Bir bölüm ekranda görüldü. Değer = bölüm anahtarı (about/stack/games/…).</summary>
    public const string Bolum = "bolum";

    /// <summary>Demo tam ekran açıldı. Değer = demo yolu.</summary>
    public const string Demo = "demo";

    /// <summary>Dış bağlantıya tıklandı. Değer = hedef host + yol.</summary>
    public const string Baglanti = "baglanti";

    /// <summary>Bölümde geçirilen süre. Değer = bölüm anahtarı, <c>SaniyeSure</c> dolu.</summary>
    public const string Sure = "sure";

    public static readonly string[] Hepsi = { Bolum, Demo, Baglanti, Sure };
}

/// <summary>Cihaz sınıfı — User-Agent'tan kabaca çıkarılır.</summary>
public static class CihazTipi
{
    public const string Masaustu = "masaustu";
    public const string Mobil = "mobil";
    public const string Tablet = "tablet";
}

/// <summary>
/// Günlük özet satırının tipi. Özet tablosu <b>bilerek esnek</b>
/// (Gun, Tip, Anahtar, Deger): yeni bir kırılım eklemek migration istemesin.
/// </summary>
public static class OzetTipi
{
    /// <summary>Anahtar = "" · Değer = o günkü toplam sayfa görüntüleme.</summary>
    public const string Toplam = "toplam";

    /// <summary>Anahtar = "" · Değer = o günkü tekil ziyaretçi.</summary>
    public const string Tekil = "tekil";

    /// <summary>Anahtar = <see cref="KaynakTipi"/> · Değer = ziyaret sayısı.</summary>
    public const string Kaynak = "kaynak";

    /// <summary>Anahtar = dil kodu · Değer = ziyaret sayısı.</summary>
    public const string Dil = "dil";

    /// <summary>Anahtar = <see cref="CihazTipi"/> · Değer = ziyaret sayısı.</summary>
    public const string Cihaz = "cihaz";

    /// <summary>Anahtar = yönlendiren host · Değer = ziyaret sayısı.</summary>
    public const string KaynakSite = "kaynaksite";

    /// <summary>Anahtar = bölüm · Değer = o bölümü GÖREN tekil ziyaretçi sayısı.</summary>
    public const string Bolum = "bolum";

    /// <summary>Anahtar = demo yolu · Değer = açılma sayısı.</summary>
    public const string Demo = "demo";

    /// <summary>Anahtar = hedef · Değer = tıklama sayısı.</summary>
    public const string Baglanti = "baglanti";

    /// <summary>Anahtar = bölüm · Değer = o bölümde geçirilen TOPLAM saniye.</summary>
    public const string Sure = "sure";
}

/// <summary>
/// Sitedeki bölümler — istemciden gelen değer bu listeye göre DOĞRULANIR.
/// <para>
/// ⚠️ Beacon ucu herkese açık: doğrulamasız kabul edersek kimse engellemeden
/// tabloya çöp (ya da çok uzun metin) yazabilir. Bilinmeyen anahtar atılır.
/// </para>
/// </summary>
public static class Bolumler
{
    // ⚠️ Değerler sayfadaki gerçek `id`'lerle AYNI (`<section id="about">` …).
    // Araya bir eşleme tablosu koymamak bilinçli: tabloyla, biri sayfada bölüm
    // adını değiştirdiğinde metrik sessizce yanlış anahtara yazmaya başlardı.
    public const string Giris = "top";
    public const string Hakkimda = "about";
    public const string Teknolojiler = "stack";
    public const string Oyunlar = "games";
    public const string Demolar = "demos";
    public const string Github = "github";
    public const string Iletisim = "contact";

    /// <summary>Sayfadaki görsel sıra — "nereye kadar indi" bu sıraya göre okunur.</summary>
    public static readonly string[] Sirali =
        { Giris, Hakkimda, Teknolojiler, Oyunlar, Demolar, Github, Iletisim };

    public static bool Gecerli(string? deger) =>
        !string.IsNullOrWhiteSpace(deger) && Array.IndexOf(Sirali, deger) >= 0;

    /// <summary>Admin ekranında gösterilecek Türkçe ad.</summary>
    public static string Ad(string bolum) => bolum switch
    {
        Giris => "Giriş",
        Hakkimda => "Hakkımda",
        Teknolojiler => "Teknolojiler",
        Oyunlar => "Oyunlar",
        Demolar => "Demolar",
        Github => "GitHub",
        Iletisim => "İletişim",
        _ => bolum
    };
}
