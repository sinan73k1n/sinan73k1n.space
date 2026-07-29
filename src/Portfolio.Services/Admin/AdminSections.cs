namespace Portfolio.Services.Admin;

/// <summary>Bir metin alanının nasıl düzenleneceği.</summary>
public enum AlanTipi { TekSatir, CokSatir }

/// <summary>Dile bağlı bir kopya anahtarı (i18n sözlüğündeki bir satır).</summary>
public sealed record KopyaAlani(string Anahtar, string Etiket, AlanTipi Tip = AlanTipi.TekSatir);

/// <summary>Bölümde hangi liste düzenlenir (yoksa <see cref="Yok"/>).</summary>
public enum ListeTipi { Yok, Loglar, Roller, Sayaclar, Teknolojiler, Oyunlar, Demolar, Depolar, Meta }

/// <summary>
/// Admin panelinin 8 bölümü — TEK KAYNAK.
/// Prototipteki sol menü sırası korunur (design_handoff live-html/admin.html).
/// Buradan hem menü, hem başlıklar, hem de düzenlenecek alanlar üretilir →
/// 8 ayrı view yazmak yerine tek şablon + bölüm tarifi (kod tekrarı yasak kuralı).
/// </summary>
public static class AdminSections
{
    public sealed record Bolum(
        string Slug,
        string Ad,
        string Aciklama,
        IReadOnlyList<KopyaAlani> Kopyalar,
        ListeTipi Liste = ListeTipi.Yok);

    public static readonly IReadOnlyList<Bolum> Hepsi = new List<Bolum>
    {
        new("genel", "Genel & terminal",
            "Site künyesi (dile bağlı değil) ve hero'daki terminal satırları.",
            Array.Empty<KopyaAlani>(), ListeTipi.Meta),

        new("banner", "Banner",
            "Hero bölümü: durum çipi, başlık, typewriter rolleri, açıklama ve butonlar.",
            new KopyaAlani[]
            {
                new("heroTag",   "Durum çipi metni"),
                new("heroLine2", "Başlığın 2. satırı (degradeli)"),
                new("heroLead",  "Açıklama paragrafı", AlanTipi.CokSatir),
                new("heroBtn1",  "1. buton (dolu)"),
                new("heroBtn2",  "2. buton (outline)"),
                new("scroll",    "Aşağı kaydır ipucu"),
                new("marquee",   "Kayan şerit metni", AlanTipi.CokSatir)
            }, ListeTipi.Roller),

        new("hakkimda", "Hakkımda",
            "Bölüm başlığı, vurgu paragrafı, gövde metni ve sayaç kutuları.",
            new KopyaAlani[]
            {
                new("aboutTitle", "Bölüm başlığı"),
                new("aboutBig",   "Vurgu paragrafı", AlanTipi.CokSatir),
                new("aboutSmall", "Gövde paragrafı", AlanTipi.CokSatir)
            }, ListeTipi.Sayaclar),

        new("teknolojiler", "Teknolojiler",
            "Teknoloji çipleri. Ad dile bağlı DEĞİL (marka adı, üç dilde aynı); NOT dile bağlıdır — her dilde ayrı yazılır.",
            new KopyaAlani[]
            {
                new("stackTitle", "Bölüm başlığı"),
                new("stackLead",  "Bölüm açıklaması", AlanTipi.CokSatir)
                // "stackSlot" (boş çip etiketi) Faz 9.4'te sayfadan kaldırıldı → artık hiçbir
                // yerde render edilmiyor. Düzenlenebilir bırakmak "yazdım ama değişmedi" tuzağı
                // olurdu. Veri anahtarı seed/JSON'da duruyor, yalnız düzenleme alanı kalktı.
            }, ListeTipi.Teknolojiler),

        new("oyunlar", "Oyunlar",
            "Google Play oyunları. Görsel yoksa kapak rengi preset'i gösterilir.",
            new KopyaAlani[]
            {
                new("gamesTitle", "Bölüm başlığı"),
                new("imgSlot",    "Görsel yer tutucu etiketi")
            }, ListeTipi.Oyunlar),

        new("demolar", "Demolar",
            "Çalışan demolar. HTML/JS alanına yapıştırılan içerik iframe içinde çalıştırılır.",
            new KopyaAlani[]
            {
                new("demosTitle", "Bölüm başlığı"),
                new("demosLead",  "Bölüm açıklaması", AlanTipi.CokSatir),
                new("demosHint",  "Önizleme altı ipucu"),
                new("demosSlot",  "Boş demo etiketi"),
                new("openDemo",   "\"Tam ekran aç\" butonu"),
                new("close",      "\"Kapat\" butonu")
            }, ListeTipi.Demolar),

        new("github", "GitHub",
            "Depo ön izlemeleri. Depo adı ve dili dile bağlı değildir.",
            new KopyaAlani[]
            {
                new("githubTitle", "Bölüm başlığı"),
                new("githubLead",  "Bölüm açıklaması", AlanTipi.CokSatir)
            }, ListeTipi.Depolar),

        new("menu", "Menü & etiketler",
            "Üst menü bağlantıları ve footer metinleri.",
            new KopyaAlani[]
            {
                new("navAbout",    "Menü: Hakkımda"),
                new("navStack",    "Menü: Teknolojiler"),
                new("navGames",    "Menü: Oyunlar"),
                new("navDemos",    "Menü: Demolar"),
                new("navGithub",   "Menü: GitHub"),
                new("contactTitle","İletişim bölüm adı"),
                new("contactHead", "İletişim başlığı"),
                new("footerNote",  "Footer notu")
            })
    };

    public static Bolum? Bul(string? slug) =>
        Hepsi.FirstOrDefault(b => string.Equals(b.Slug, slug, StringComparison.OrdinalIgnoreCase));

    /// <summary>Tüm bölümlerdeki kopya anahtarları (boş alan sayacı bunları tarar).</summary>
    public static IEnumerable<string> TumKopyaAnahtarlari =>
        Hepsi.SelectMany(b => b.Kopyalar).Select(k => k.Anahtar);
}
