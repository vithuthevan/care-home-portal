using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;

namespace CareHome.Api.Security;

public sealed class LoginPasswordCipher(IHostEnvironment environment) : IDisposable
{
    public const string Prefix = "enc:";

    private readonly RSA _rsa = RSA.Create(2048);
    private readonly bool _allowPlaintext = environment.IsDevelopment();

    public LoginPublicKeyDto PublicKey()
    {
        var parameters = _rsa.ExportParameters(false);
        return new LoginPublicKeyDto
        {
            Kty = "RSA",
            N = ToBase64Url(parameters.Modulus!),
            E = ToBase64Url(parameters.Exponent!),
            Alg = "RSA-OAEP-256",
            Ext = true,
            KeyOps = ["encrypt"]
        };
    }

    public bool TryResolve(string? cipher, string? plaintext, out string password)
    {
        if (!string.IsNullOrWhiteSpace(cipher))
        {
            return TryDecrypt(cipher, out password);
        }

        // Plaintext body passwords are Development-only (tests / emergency tooling).
        if (_allowPlaintext)
        {
            password = plaintext ?? string.Empty;
            return password.Length > 0;
        }

        password = string.Empty;
        return false;
    }

    public bool TryDecrypt(string? cipher, out string password)
    {
        password = string.Empty;
        if (string.IsNullOrWhiteSpace(cipher)
            || !cipher.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var bytes = Convert.FromBase64String(cipher[Prefix.Length..]);
            password = Encoding.UTF8.GetString(_rsa.Decrypt(bytes, RSAEncryptionPadding.OaepSHA256));
            return password.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public void Dispose() => _rsa.Dispose();

    internal string EncryptForTests(string password)
    {
        var bytes = _rsa.Encrypt(Encoding.UTF8.GetBytes(password), RSAEncryptionPadding.OaepSHA256);
        return Prefix + Convert.ToBase64String(bytes);
    }

    private static string ToBase64Url(byte[] data)
    {
        return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

public sealed class LoginPublicKeyDto
{
    [JsonPropertyName("kty")]
    public string Kty { get; set; } = "RSA";

    [JsonPropertyName("n")]
    public string N { get; set; } = string.Empty;

    [JsonPropertyName("e")]
    public string E { get; set; } = string.Empty;

    [JsonPropertyName("alg")]
    public string Alg { get; set; } = "RSA-OAEP-256";

    [JsonPropertyName("ext")]
    public bool Ext { get; set; } = true;

    [JsonPropertyName("key_ops")]
    public string[] KeyOps { get; set; } = ["encrypt"];
}
