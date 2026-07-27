namespace Portfolio.Entities;

/// <summary>
/// Desteklenen diller.
/// <para>
/// ⭐ <b>İKİ AYRI KAVRAM — karıştırma:</b>
/// </para>
/// <list type="bullet">
/// <item>
///   <see cref="Fallback"/> = <b>TR</b> — içeriğin yazıldığı dil. Bir çeviri eksikse
///   metin buraya düşer; "Boşları doldur" bundan kopyalar; JSON içe aktarımı bunun
///   varlığını arar. <b>İçerik Türkçe yazıldığı sürece değişmez.</b>
/// </item>
/// <item>
///   <see cref="Initial"/> = <b>EN</b> — siteye <b>ilk kez</b> gelen ziyaretçinin
///   göreceği dil (çerezi yoksa). Sonra kullanıcı dili değiştirirse tercihi çerezde
///   saklanır ve sonraki girişlerde o dil gelir. Bu bir <b>sunum</b> tercihidir,
///   içerikle ilgisi yoktur (karar: 2026-07-27).
/// </item>
/// </list>
/// Bu ikisi ayrı olmasaydı: ilk dil EN yapılınca eksik EN çevirileri boş görünürdü
/// ve admin'deki "TR'den doldur" düğmesi İngilizceden kopyalamaya başlardı.
/// </summary>
public static class Lang
{
    public const string Tr = "tr";
    public const string En = "en";
    public const string Ru = "ru";

    /// <summary>Görüntüleme sırası (dil anahtarındaki sıra).</summary>
    public static readonly string[] All = { Tr, En, Ru };

    /// <summary>Eksik çevirinin düşeceği dil — içeriğin yazıldığı dil.</summary>
    public const string Fallback = Tr;

    /// <summary>Çerezi olmayan ziyaretçiye açılacak dil.</summary>
    public const string Initial = En;

    /// <summary>Verilen kod desteklenen bir dil mi?</summary>
    public static bool Gecerli(string? deger) =>
        !string.IsNullOrWhiteSpace(deger) && Array.IndexOf(All, deger.Trim().ToLowerInvariant()) >= 0;

    /// <summary>
    /// Serbest girdiyi güvenli dil koduna çevirir (bilinmeyen → <see cref="Fallback"/>).
    /// İçerik çözümlemesinde kullanılır: kullanıcı girdisi doğrudan sözlük anahtarı olmaz.
    /// ⚠️ Ziyaretçiye HANGİ dilin açılacağına burası karar vermez — orası
    /// <see cref="Initial"/> + çerez işi (bkz. HomeController).
    /// </summary>
    public static string Normalize(string? deger) =>
        Gecerli(deger) ? deger!.Trim().ToLowerInvariant() : Fallback;
}
