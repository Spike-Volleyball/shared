using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Options;
using Shared.Services.Logging;

namespace Shared.Services;

public class EmailService : BaseEmailService
{
    private readonly IAmazonSimpleEmailServiceV2 _sesClient;

    public EmailService(
        IAmazonSimpleEmailServiceV2 sesClient,
        ILogger<EmailService> logger,
        IOptions<EmailSettings> emailOptions,
        ILogPseudonymizer pseudonymizer)
        : base(logger, emailOptions, pseudonymizer)
    {
        _sesClient = sesClient;
    }

    protected override async Task SendEmailCoreAsync(string toEmail, string fromEmail, string subject, string htmlBody)
    {
        var request = new SendEmailRequest
        {
            FromEmailAddress = fromEmail,
            Destination = new Destination
            {
                ToAddresses = new List<string> { toEmail }
            },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content
                    {
                        Data = subject,
                        Charset = "UTF-8"
                    },
                    Body = new Body
                    {
                        Html = new Content
                        {
                            Data = htmlBody,
                            Charset = "UTF-8"
                        }
                    }
                }
            }
        };

        var response = await _sesClient.SendEmailAsync(request);
        _logger.LogDebug("SES MessageId: {MessageId}", response.MessageId);
    }
}
