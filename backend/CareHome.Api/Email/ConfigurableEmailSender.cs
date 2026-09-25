using System.Net;
using System.Net.Mail;
using CareHome.Api.Telemetry;
using Microsoft.Extensions.Options;

namespace CareHome.Api.Email;

public class ConfigurableEmailSender(
    IOptions<EmailOptions> emailOptions,
    IHostEnvironment environment,
    ILogger<ConfigurableEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = emailOptions.Value;

    public async Task<EmailSendResult> SendAsync(
        string to,
        string subject,
        string body,
        string? attachmentFileName,
        byte[]? attachmentBytes,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsSmtpMode)
        {
            return HandleSimulatedSend(to, subject, attachmentFileName);
        }

        var host = _options.Smtp.Host;
        if (string.IsNullOrWhiteSpace(host))
        {
            return new EmailSendResult
            {
                Success = false,
                Simulated = false,
                ErrorMessage = "SMTP host is not configured. Set Email__Smtp__Host."
            };
        }

        if (string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            return new EmailSendResult
            {
                Success = false,
                Simulated = false,
                ErrorMessage = "From address is not configured. Set Email__FromAddress."
            };
        }

        if (!string.IsNullOrWhiteSpace(_options.Smtp.User)
            && string.IsNullOrWhiteSpace(_options.Smtp.Password))
        {
            return new EmailSendResult
            {
                Success = false,
                Simulated = false,
                ErrorMessage = "SMTP username is set but password is missing. Configure Email__Smtp__Password via Key Vault or environment."
            };
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(to);

            if (attachmentBytes is not null && !string.IsNullOrWhiteSpace(attachmentFileName))
            {
                var stream = new MemoryStream(attachmentBytes);
                message.Attachments.Add(new Attachment(stream, attachmentFileName, "application/pdf"));
            }

            using var client = new SmtpClient(host)
            {
                Port = _options.Smtp.Port > 0 ? _options.Smtp.Port : 587,
                EnableSsl = _options.Smtp.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(_options.Smtp.User))
            {
                client.Credentials = new NetworkCredential(_options.Smtp.User, _options.Smtp.Password);
            }

            await client.SendMailAsync(message, cancellationToken);

            logger.LogInformation(
                "Email sent via SMTP to {Recipient} subject {Subject} attachment {Attachment}",
                to,
                subject,
                attachmentFileName ?? "(none)");

            return new EmailSendResult { Success = true };
        }
        catch (Exception ex)
        {
            var safeDetail = EmailLogSanitizer.SanitizeForLog(
                ex.Message,
                _options.Smtp.Password,
                _options.Smtp.User);

            CareHomeTelemetry.EmailSendFailures.Add(1);
            logger.LogError(
                ex,
                "SMTP send failed for {Recipient}. {Summary} Detail={Detail}",
                to,
                EmailLogSanitizer.DescribeSmtpFailure(ex),
                safeDetail);

            return new EmailSendResult
            {
                Success = false,
                Simulated = false,
                ErrorMessage = EmailLogSanitizer.DescribeSmtpFailure(ex)
            };
        }
    }

    private EmailSendResult HandleSimulatedSend(string to, string subject, string? attachmentFileName)
    {
        if (environment.IsDevelopment())
        {
            logger.LogWarning(
                "EMAIL SIMULATED (Email:Mode={Mode}). To={To} Subject={Subject} Attachment={Attachment}. No message was delivered.",
                _options.Mode,
                to,
                subject,
                attachmentFileName);

            return new EmailSendResult { Success = true, Simulated = true };
        }

        logger.LogWarning(
            "EMAIL NOT DELIVERED (Email:Mode={Mode}). To={To} Subject={Subject}. Production requires Email__Mode=Smtp for live delivery.",
            string.IsNullOrWhiteSpace(_options.Mode) ? "(empty)" : _options.Mode,
            to,
            subject);

        return new EmailSendResult
        {
            Success = false,
            Simulated = true,
            ErrorMessage =
                "Email delivery is not configured. Set Email__Mode=Smtp with SMTP settings (see docs/operations/PRODUCTION_EMAIL_SETUP.md), " +
                "or use the approved manual-send workflow if Email__AllowSimulationInProduction is enabled."
        };
    }
}
