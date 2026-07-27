using Microsoft.Extensions.Options;

namespace Portfolio.Services.Auth;

/// <summary>Login formundaki (kullanıcı + şifre + TOTP) doğrulaması. İKİ FAKTÖR DE geçmeli.</summary>
public interface IAdminAuthenticator
{
    bool Enabled { get; }
    string Username { get; }
    int SessionHours { get; }

    /// <summary>Şifre VE TOTP doğruysa true. Hangi alanın yanlış olduğu DIŞARI VERİLMEZ.</summary>
    bool Validate(string? username, string? password, string? totpCode);
}

public sealed class AdminAuthenticator : IAdminAuthenticator
{
    private readonly AuthOptions _opt;

    public AdminAuthenticator(IOptions<AuthOptions> options) => _opt = options.Value;

    public bool Enabled => _opt.Enabled;
    public string Username => _opt.Username;
    public int SessionHours => _opt.SessionHours;

    public bool Validate(string? username, string? password, string? totpCode)
    {
        if (!_opt.Enabled) return false;

        // Üç kontrol de HER ZAMAN çalıştırılır (kullanıcı adı yanlış olsa bile PBKDF2 ve
        // TOTP hesaplanır) → "hangi alan yanlıştı" bilgisi çalışma süresinden sızmasın.
        var userOk = string.Equals(username?.Trim(), _opt.Username, StringComparison.OrdinalIgnoreCase);
        var passOk = PasswordHasher.Verify(password ?? "", _opt.PasswordHash);
        var totpOk = Totp.Verify(_opt.TotpSecret!, totpCode);

        return userOk && passOk && totpOk;
    }
}
