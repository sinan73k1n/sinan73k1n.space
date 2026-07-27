using System.Security.Cryptography;

namespace Portfolio.Services.Auth;

/// <summary>
/// PBKDF2-SHA256 şifre hash'leme (BCL — harici bağımlılık yok). Format:
/// <c>pbkdf2$&lt;iterations&gt;$&lt;saltB64&gt;$&lt;hashB64&gt;</c>. Şifre ASLA düz saklanmaz;
/// env dosyasında yalnız bu hash durur. Doğrulama sabit-zamanlı.
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 210_000;   // OWASP 2023 (PBKDF2-SHA256) tavsiyesi
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"pbkdf2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string? encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded)) return false;
        var parts = encoded.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2") return false;
        if (!int.TryParse(parts[1], out var iterations)) return false;

        byte[] salt, expected;
        try { salt = Convert.FromBase64String(parts[2]); expected = Convert.FromBase64String(parts[3]); }
        catch { return false; }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
