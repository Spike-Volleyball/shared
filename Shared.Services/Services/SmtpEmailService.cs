using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Shared.Options;
using Shared.Services.Logging;

namespace Shared.Services;

public class SmtpEmailService : BaseEmailService
{
    public SmtpEmailService(
        ILogger<SmtpEmailService> logger,
        IOptions<EmailSettings> emailOptions,
        ILogPseudonymizer pseudonymizer)
        : base(logger, emailOptions, pseudonymizer)
    {
        if (string.IsNullOrWhiteSpace(emailOptions.Value.SmtpHost))
            throw new ArgumentException("SmtpHost must be configured when using SMTP transport.", nameof(emailOptions));
    }

    protected override async Task SendEmailCoreAsync(string toEmail, string fromEmail, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var client = new SmtpClient();

        var tlsOption = _emailOptions.SmtpUseTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        await client.ConnectAsync(_emailOptions.SmtpHost!, _emailOptions.SmtpPort, tlsOption, cts.Token);
        await client.SendAsync(message, cts.Token);
        await client.DisconnectAsync(true, cts.Token);
    }
}
