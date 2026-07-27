namespace Portfolio.Services.Auth;

/// <summary>
/// "Auth" bölümünden bağlanır. Sırlar (şifre hash'i + TOTP secret) **repoda DURMAZ**;
/// sunucuda env dosyasından gelir:
/// <c>Auth__Username</c>, <c>Auth__PasswordHash</c>, <c>Auth__TotpSecret</c>.
/// <para>
/// <b>Etkinleşme:</b> <see cref="Enabled"/> = PasswordHash + TotpSecret dolu mu.
/// Mac'te env yok → auth KAPALI, /admin geliştirme için login'siz açılır.
/// Sunucuda env var → AÇIK. Production'da kapalıysa Program.cs uygulamayı BAŞLATMAZ
/// (AdminPanel'den fark: orada uyarı yeterliydi çünkü panel Tailscale arkasındaydı;
///  burada admin İNTERNETE AÇIK, korumasız açılması kabul edilemez).
/// </para>
/// </summary>
public class AuthOptions
{
    public const string SectionName = "Auth";

    public string Username { get; set; } = "admin";

    /// <summary>PBKDF2 hash (<see cref="PasswordHasher"/> formatı). Boşsa auth kapalı.</summary>
    public string? PasswordHash { get; set; }

    /// <summary>Base32 TOTP secret. Boşsa auth kapalı.</summary>
    public string? TotpSecret { get; set; }

    /// <summary>Oturum çerezi ömrü (saat).</summary>
    public int SessionHours { get; set; } = 12;

    // ---- Kaba kuvvet koruması (bu projeye ÖZEL: admin public) ----

    /// <summary>Kilit devreye girmeden önceki başarısız deneme sayısı (IP başına).</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>İlk kilit süresi (saniye). Her tekrar kilitte katlanarak artar.</summary>
    public int LockoutSeconds { get; set; } = 60;

    /// <summary>Kilit süresinin üst sınırı (saniye) — sonsuza kadar büyümesin.</summary>
    public int MaxLockoutSeconds { get; set; } = 3600;

    public bool Enabled =>
        !string.IsNullOrWhiteSpace(PasswordHash) && !string.IsNullOrWhiteSpace(TotpSecret);
}
