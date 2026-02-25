using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Scriptube.Automation.Api.Utilities;

/// <summary>
/// Utilities for computing and verifying HMAC-SHA256 signatures,
/// as used by the Scriptube webhook delivery (<c>X-Scriptube-Signature</c> header).
/// </summary>
/// <remarks>
/// Scriptube canonicalises the JSON payload before signing: keys are sorted
/// recursively and the output uses Python's default separators (<c>", "</c> between
/// items, <c>": "</c> between key and value), equivalent to
/// <c>json.dumps(payload, sort_keys=True)</c> in Python.
/// Always pass <see cref="CanonicalizeJson"/> output (not the raw body string)
/// to <see cref="Compute"/> when verifying Scriptube webhook signatures.
/// </remarks>
public static class HmacVerifier
{
    // Relaxed encoder prevents over-escaping of characters like '+' that Python's
    // json.dumps leaves unescaped, ensuring the canonical form matches Scriptube's.
    private static readonly JsonSerializerOptions _relaxedOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Re-serialises a JSON string with recursively-sorted keys and Python-compatible
    /// separators (<c>", "</c> / <c>": "</c>), matching Scriptube's signing canonical form.
    /// </summary>
    public static string CanonicalizeJson(string json)
    {
        var node = JsonNode.Parse(json)
            ?? throw new ArgumentException("Input is not valid JSON.", nameof(json));
        return SerializeSorted(node);
    }

    private static string SerializeSorted(JsonNode? node)
    {
        return node switch
        {
            JsonObject obj =>
                "{" + string.Join(", ",
                    obj.OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                       .Select(kvp => $"\"{kvp.Key}\": {SerializeSorted(kvp.Value)}"))
                + "}",

            JsonArray arr =>
                "[" + string.Join(", ", arr.Select(SerializeSorted)) + "]",

            JsonValue val => val.ToJsonString(_relaxedOptions),

            null => "null",

            _ => node.ToJsonString(_relaxedOptions)
        };
    }

    /// <summary>
    /// Computes the HMAC-SHA256 signature for <paramref name="payload"/> using <paramref name="secret"/>
    /// and returns it as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="secret">The webhook signing secret registered with Scriptube.</param>
    /// <param name="payload">
    /// The canonicalised payload string. For Scriptube webhooks, pass the output of
    /// <see cref="CanonicalizeJson"/> rather than the raw body bytes.
    /// </param>
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
    /// <param name="payload">The canonicalised payload string.</param>
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
