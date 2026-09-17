using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Options;
using Shared.Services.Interfaces;
using Shared.Services.Logging;

namespace Shared.Services;

public abstract class BaseEmailService : IEmailService
{
    protected readonly ILogger _logger;
    protected readonly EmailSettings _emailOptions;
    private readonly ILogPseudonymizer _pseudonymizer;
    private readonly string _templatesPath;

    protected BaseEmailService(ILogger logger, IOptions<EmailSettings> emailOptions, ILogPseudonymizer pseudonymizer)
    {
        _logger = logger;
        _emailOptions = emailOptions.Value;
        _pseudonymizer = pseudonymizer;
        _templatesPath = Path.Combine(AppContext.BaseDirectory, "EmailTemplates");
    }

    public async Task SendEmailAsync(string email, string templateName, Dictionary<string, string> parameters)
    {
        try
        {
            var template = await LoadTemplateAsync(templateName);
            var processedTemplate = ProcessTemplate(template, parameters);

            await SendEmailCoreAsync(email, _emailOptions.FromEmail, processedTemplate.Subject, processedTemplate.Body);

            _logger.LogInformation("Email sent successfully to {EmailRef} using template {TemplateName}",
                _pseudonymizer.PseudonymizeEmail(email), templateName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {EmailRef} using template {TemplateName}",
                _pseudonymizer.PseudonymizeEmail(email), templateName);
            throw;
        }
    }

    protected abstract Task SendEmailCoreAsync(string toEmail, string fromEmail, string subject, string htmlBody);

    private async Task<EmailTemplate> LoadTemplateAsync(string templateName)
    {
        var templateDirectory = Path.Combine(_templatesPath, templateName);
        var htmlFile = Path.Combine(templateDirectory, "template.html");
        var subjectFile = Path.Combine(templateDirectory, "subject.txt");

        // Read both files in parallel — they're independent I/O
        var bodyTask = File.ReadAllTextAsync(htmlFile);
        var subjectTask = File.Exists(subjectFile)
            ? File.ReadAllTextAsync(subjectFile)
            : Task.FromResult(string.Empty);

        try
        {
            await Task.WhenAll(bodyTask, subjectTask);
        }
        catch (FileNotFoundException ex)
        {
            throw new FileNotFoundException($"Email template not found: {ex.FileName}", ex);
        }

        return new EmailTemplate
        {
            Body = bodyTask.Result,
            Subject = subjectTask.Result,
        };
    }

    private EmailTemplate ProcessTemplate(EmailTemplate template, Dictionary<string, string> parameters)
    {
        return new EmailTemplate
        {
            Subject = ProcessString(template.Subject, parameters),
            Body = ProcessString(template.Body, parameters),
        };
    }

    private static string ProcessString(string input, Dictionary<string, string> parameters)
    {
        if (string.IsNullOrEmpty(input) || parameters == null)
            return input;

        var result = input;
        foreach (var parameter in parameters)
        {
            result = result.Replace($"{{{{{parameter.Key}}}}}", parameter.Value);
        }

        return result;
    }

    protected class EmailTemplate
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}
