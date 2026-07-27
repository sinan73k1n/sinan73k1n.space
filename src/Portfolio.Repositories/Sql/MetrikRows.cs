namespace Portfolio.Repositories.Sql;

/// <summary>
/// Tek bir sayfa görüntüleme. **Sunucu tarafında** kaydedilir — JS kapalıyken de,
/// reklam engelleyici varken de sayılır (kendi sitemiz, kendi isteğimiz).
///
/// <para>
/// 🔒 <b>Ham IP SAKLANMAZ.</b> <see cref="ZiyaretciHash"/> = SHA-256(IP + tarayıcı +
/// o güne özel rastgele tuz). Tuz her gece değiştiği için aynı kişi ertesi gün
/// başka bir kimliğe düşer: günler arası takip <b>yapısal olarak</b> mümkün değil.
/// Bu yüzden çerez de yok, dolayısıyla çerez onay bannerine gerek yok.
/// </para>
/// </summary>
public sealed class ZiyaretRow
{
    public long Id { get; set; }

    public DateTime ZamanUtc { get; set; }

    /// <summary>Gün (UTC, saat bileşeni yok) — özetleme ve temizlik bu kolona bakar.</summary>
    public DateOnly Gun { get; set; }

    /// <summary>Çerezsiz günlük kimlik. Ham IP'ye geri döndürülemez.</summary>
    public string ZiyaretciHash { get; set; } = "";

    /// <summary>İstenen yol (ör. "/"). Site tek sayfa ama admin/demo ayrımı için tutulur.</summary>
    public string Yol { get; set; } = "";

    public string Dil { get; set; } = "";

    /// <summary>organik · dogrudan · sosyal · yonlendirme</summary>
    public string KaynakTipi { get; set; } = "";

    /// <summary>Yalnız host (ör. "google.com"). Tam URL saklanmaz — gereksiz kişisel veri.</summary>
    public string KaynakHost { get; set; } = "";

    public string Cihaz { get; set; } = "";
}

/// <summary>
/// Sayfa içi olay: bölüm görüldü · demo açıldı · dış bağlantı · bölümde geçen süre.
/// İstemciden <c>navigator.sendBeacon</c> ile gelir.
/// </summary>
public sealed class OlayRow
{
    public long Id { get; set; }

    public DateTime ZamanUtc { get; set; }
    public DateOnly Gun { get; set; }
    public string ZiyaretciHash { get; set; } = "";

    /// <summary>bolum · demo · baglanti · sure</summary>
    public string Tip { get; set; } = "";

    /// <summary>Tipe göre: bölüm anahtarı, demo yolu ya da bağlantı hedefi.</summary>
    public string Deger { get; set; } = "";

    /// <summary>Yalnız <c>sure</c> tipinde dolu: o bölümde geçirilen saniye.</summary>
    public int SaniyeSure { get; set; }
}

/// <summary>
/// Günlük özet — <b>kalıcı</b>. Ham satırlar 90 gün sonra silinir, bu kalır;
/// böylece yıllar arası karşılaştırma yapılabilirken DB ve yedek şişmez.
///
/// <para>
/// Şema bilerek esnek (Gun, Tip, Anahtar, Deger): yeni bir kırılım eklemek
/// (ör. ülke) migration gerektirmesin.
/// </para>
/// </summary>
public sealed class GunlukOzetRow
{
    public long Id { get; set; }

    public DateOnly Gun { get; set; }

    /// <summary>toplam · tekil · kaynak · dil · cihaz · bolum · demo · baglanti · sure</summary>
    public string Tip { get; set; } = "";

    /// <summary>Kırılım anahtarı; toplam/tekil için boş.</summary>
    public string Anahtar { get; set; } = "";

    public int Deger { get; set; }
}

/// <summary>
/// Ziyaretçi kimliğini üretmekte kullanılan GÜNLÜK tuz.
/// <para>
/// Neden tabloda: tuz gün içinde sabit kalmalı (aynı ziyaretçi aynı kimliği almalı)
/// ama gün dönünce değişmeli. Bellekte tutulsaydı uygulama her yeniden başladığında
/// değişir, aynı gün içindeki tekil sayımı bozardı.
/// </para>
/// <para>
/// Ham veriyle birlikte silinir: tuz gidince o günün hash'leri kimseye
/// bağlanamaz hâle gelir — geriye dönük çözülemezlik.
/// </para>
/// </summary>
public sealed class GunlukTuzRow
{
    public DateOnly Gun { get; set; }
    public string Tuz { get; set; } = "";
}
