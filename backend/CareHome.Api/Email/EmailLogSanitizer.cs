using System.Text.RegularExpressions;

namespace CareHome.Api.Email;

internal static partial class EmailLogSanitizer
{
    public static string SanitizeForLog(string? message, params string?[] secrets)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        var sanitized = message;
        foreach (var secret in secrets)
        {
            if (!string.IsNullOrEmpty(secret))
            {
                sanitized = sanitized.Replace(secret, "***", StringComparison.Ordinal);
            }
        }

        sanitized = CredentialPattern().Replace(sanitized, "$1=***");
        return sanitized;
    }

    public static string DescribeSmtpFailure(Exception ex)
    {
        for (Exception? current = ex; current is not null; current = current.InnerException)
        {
            var described = DescribeOne(current);
            if (described is not null)
            {
                return described;
            }
        }

        return "SMTP send failed. Verify Email__Smtp__Host, Email__Smtp__Port, Email__Smtp__User, Email__Smtp__Password, and Email__Smtp__EnableSsl.";
    }

    private static string? DescribeOne(Exception ex)
    {
        return ex switch
        {
            System.Net.Mail.SmtpFailedRecipientsException =>
                "SMTP server rejected one or more recipients.",
            System.Net.Mail.SmtpException =>
                "SMTP server returned an error. Verify host, port, TLS, and credentials.",
            System.Security.Authentication.AuthenticationException =>
                "SMTP authentication failed. Verify username and password.",
            System.Net.Sockets.SocketException =>
                "Could not connect to the SMTP server. Verify host, port, and network access.",
            TimeoutException =>
                "SMTP connection timed out. Verify host, port, and network access.",
            _ => null
        };
    }

    [GeneratedRegex(@"(password|pwd|pass|secret|token)\s*[=:]\s*[^\s;,'""]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CredentialPattern();
}
