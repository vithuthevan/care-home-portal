using CareHome.Api.Security;
using Xunit;

namespace CareHome.Api.Tests;

public class LoginPasswordCipherTests
{
    [Fact]
    public void Encrypted_payload_does_not_contain_the_password()
    {
        using var cipher = new LoginPasswordCipher();
        const string password = "Test password";

        var encrypted = cipher.EncryptForTests(password);

        Assert.StartsWith(LoginPasswordCipher.Prefix, encrypted);
        Assert.DoesNotContain(password, encrypted);
        Assert.True(cipher.TryDecrypt(encrypted, out var decrypted));
        Assert.Equal(password, decrypted);
        Assert.True(cipher.TryResolve(encrypted, "ignored", out var fromCipher));
        Assert.Equal(password, fromCipher);
        Assert.True(cipher.TryResolve(null, password, out var fromPlain));
        Assert.Equal(password, fromPlain);
    }

    [Fact]
    public void Invalid_cipher_is_rejected()
    {
        using var cipher = new LoginPasswordCipher();

        Assert.False(cipher.TryDecrypt("Test password", out _));
        Assert.False(cipher.TryDecrypt("enc:not-valid-base64", out _));
        Assert.False(cipher.TryDecrypt(null, out _));
        Assert.False(cipher.TryResolve("enc:not-valid-base64", null, out _));
    }
}
