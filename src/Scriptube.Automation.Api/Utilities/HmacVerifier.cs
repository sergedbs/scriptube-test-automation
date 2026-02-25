using System.Security.Cryptography;
using System.Text;

namespace Scriptube.Automation.Api.Utilities;

/// <summary>
/// Utilities for computing and verifying HMAC-SHA256 signatures,
/// as used by the Scriptube webhook delivery (<c>X-Scriptube-Signature</c> header).
/// </summary>
public static class HmacVerifier
{
    /// <summary>
    /// Computes the HMAC-SHA256 signature for <paramref name="payload"/> using <paramref name="secret"/>
    /// and returns it as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="secret">The webhook signing secret registered with Scriptube.</param>
    /// <param name="payload">The raw UTF-8 request body received from Scriptube.</param>
    /// <returns>Lowercase hex-encoded HMAC-SHA256 digest.</returns>
    public static string Compute(string secret, string payload)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        var hash = HMACSHA256.HashData(keyBytes, payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Verifies that <paramref name="signature"/> matches the HMAC-SHA256 of <paramref name="payload"/>
    /// signed with <paramref name="secret"/>.
    /// Uses a constant-time comparison to prevent timing attacks.
    /// </summary>
    /// <param name="secret">The webhook signing secret.</param>
    /// <param name="payload">The raw UTF-8 request body.</param>
    /// <param name="signature">The signature from the <c>X-Scriptube-Signature</c> header.</param>
    /// <returns><see langword="true"/> if the signature is valid; otherwise <see langword="false"/>.</returns>
    public static bool Verify(string secret, string payload, string signature)
    {
        var expected = Compute(secret, payload);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }
}
