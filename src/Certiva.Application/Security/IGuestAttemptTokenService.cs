namespace Certiva.Application.Security;

public interface IGuestAttemptTokenService
{
    string GenerateToken();
    string HashToken(string token);
    bool Matches(string token, string? storedHash);
}

public sealed class GuestAttemptTokenService : IGuestAttemptTokenService
{
    public string GenerateToken() => Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public string HashToken(string token) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
    public bool Matches(string token, string? storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash)) return false;
        try { return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(Convert.FromHexString(HashToken(token)), Convert.FromHexString(storedHash)); }
        catch (FormatException) { return false; }
    }
}
