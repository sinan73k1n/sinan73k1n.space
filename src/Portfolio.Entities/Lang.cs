namespace Portfolio.Entities;

/// <summary>
/// Desteklenen diller. TR varsayılan ve **fallback**: eksik çeviri TR'ye düşer.
/// Yeni dil eklemek = buraya kod eklemek + seed/DB'ye çeviri satırı (şema değişmez).
/// </summary>
public static class Lang
{
    public const string Tr = "tr";
    public const string En = "en";
    public const string Ru = "ru";

    /// <summary>Görüntüleme sırası (dil anahtarındaki sıra).</summary>
    public static readonly string[] All = { Tr, En, Ru };

    public const string Default = Tr;

    /// <summary>
    /// Serbest girdiyi güvenli dil koduna çevirir (bilinmeyen → TR).
    /// Kullanıcı girdisi doğrudan sözlük anahtarı olarak kullanılmaz.
    /// </summary>
    public static string Normalize(string? deger)
    {
        if (string.IsNullOrWhiteSpace(deger)) return Default;
        var k = deger.Trim().ToLowerInvariant();
        return Array.IndexOf(All, k) >= 0 ? k : Default;
    }
}
