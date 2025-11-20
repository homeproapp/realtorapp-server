using System.Reflection;
using System.Web;
using Amazon;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Logging;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Settings;

namespace RealtorApp.Domain.Services;

public class EmailService(AppSettings appSettings, ILogger<EmailService> logger) : IEmailService
{
    private readonly AppSettings _appSettings = appSettings;
    private readonly ILogger<EmailService> _logger = logger;
    private static string? _newClientTemplate;
    private static string? _existingClientTemplate;

    public async Task<bool> SendInvitationEmailAsync(string clientEmail, string? clientFirstName, string agentName, string encryptedData, bool isExistingUser)
    {
        try
        {
            var invitationLink = GenerateInvitationLink(encryptedData);
            var emailBody = isExistingUser
                    ? GenerateExistingClientInvitationEmailBody(clientFirstName, agentName, invitationLink, _appSettings.AppName)
                    : GenerateNewClientInvitationEmailBody(clientFirstName, agentName, invitationLink, _appSettings.AppName);
            var credentials = new Amazon.Runtime.BasicAWSCredentials(
                _appSettings.Aws.AccessKey,
                _appSettings.Aws.SecretKey
            );

            var region = RegionEndpoint.GetBySystemName(_appSettings.Aws.Ses.Region);
            using var client = new AmazonSimpleEmailServiceV2Client(credentials, region);

            var htmlBody = isExistingUser
                ? GenerateExistingClientInvitationEmailHtml(clientFirstName, agentName, invitationLink, _appSettings.AppName)
                : GenerateNewClientInvitationEmailHtml(clientFirstName, agentName, invitationLink, _appSettings.AppName);

            var sendRequest = new SendEmailRequest
            {
                FromEmailAddress = $"{_appSettings.Aws.Ses.FromName} <{_appSettings.Aws.Ses.FromEmail}>",
                Destination = new Destination
                {
                    ToAddresses = [clientEmail]
                },
                Content = new EmailContent
                {
                    Simple = new Message
                    {
                        Subject = new Content
                        {
                            Data = $"You've been invited to {_appSettings.AppName} by {agentName}",
                            Charset = "UTF-8"
                        },
                        Body = new Body
                        {
                            Text = new Content
                            {
                                Data = emailBody,
                                Charset = "UTF-8"
                            },
                            Html = new Content
                            {
                                Data = htmlBody,
                                Charset = "UTF-8"
                            }
                        }
                    }
                }
            };

            await client.SendEmailAsync(sendRequest);

            _logger.LogInformation("Successfully sent invitation email to {ClientEmail} from {AgentName}", clientEmail, agentName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invitation email to {ClientEmail}", clientEmail);
            return false;
        }
    }

    public async Task<List<InvitationEmailDto>> SendBulkInvitationEmailsAsync(List<InvitationEmailDto> invitations)
    {
        var failedInvites = new List<InvitationEmailDto>();

        var credentials = new Amazon.Runtime.BasicAWSCredentials(
            _appSettings.Aws.AccessKey,
            _appSettings.Aws.SecretKey
        );

        var region = RegionEndpoint.GetBySystemName(_appSettings.Aws.Ses.Region);
        using var client = new AmazonSimpleEmailServiceV2Client(credentials, region);

        foreach (var invitation in invitations)
        {
            try
            {
                var invitationLink = GenerateInvitationLink(invitation.EncryptedData);
                var emailBody = invitation.IsExistingUser
                    ? GenerateExistingClientInvitationEmailBody(invitation.ClientFirstName, invitation.AgentName, invitationLink, _appSettings.AppName)
                    : GenerateNewClientInvitationEmailBody(invitation.ClientFirstName, invitation.AgentName, invitationLink, _appSettings.AppName);

                var htmlBody = invitation.IsExistingUser
                    ? GenerateExistingClientInvitationEmailHtml(invitation.ClientFirstName, invitation.AgentName, invitationLink, _appSettings.AppName)
                    : GenerateNewClientInvitationEmailHtml(invitation.ClientFirstName, invitation.AgentName, invitationLink, _appSettings.AppName);

                var sendRequest = new SendEmailRequest
                {
                    FromEmailAddress = $"{_appSettings.Aws.Ses.FromName} <{_appSettings.Aws.Ses.FromEmail}>",
                    Destination = new Destination
                    {
                        ToAddresses = [invitation.ClientEmail]
                    },
                    Content = new EmailContent
                    {
                        Simple = new Message
                        {
                            Subject = new Content
                            {
                                Data = $"You've been invited to {_appSettings.AppName} by {invitation.AgentName}",
                                Charset = "UTF-8"
                            },
                            Body = new Body
                            {
                                Text = new Content
                                {
                                    Data = emailBody,
                                    Charset = "UTF-8"
                                },
                                Html = new Content
                                {
                                    Data = htmlBody,
                                    Charset = "UTF-8"
                                }
                            }
                        }
                    }
                };

                await client.SendEmailAsync(sendRequest);

                _logger.LogInformation("Successfully sent invitation email to {ClientEmail} from {AgentName}",
                    invitation.ClientEmail, invitation.AgentName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invitation email to {ClientEmail}", invitation.ClientEmail);
                failedInvites.Add(invitation);
            }
        }

        return failedInvites;
    }

    private string GenerateInvitationLink(string encryptedData)
    {
        return $"{_appSettings.ClientUrl}/invitations/accept?data={HttpUtility.UrlEncode(encryptedData)}";
    }

    private static string GenerateNewClientInvitationEmailBody(string? clientFirstName, string agentName, string invitationLink, string applicationName)
    {
        var greeting = string.IsNullOrWhiteSpace(clientFirstName) ? "Hello," : $"Hello {clientFirstName},";

        return $@"{greeting}

You've been invited by {agentName} to join {applicationName}. We're excited to help you with your listing needs!

To accept this invitation and create your account, please click the link below:

{invitationLink}

This invitation will expire in 7 days.

If you have any questions, please don't hesitate to reach out to {agentName}.

Best regards,
The {applicationName} Team";
    }

    private static string GenerateExistingClientInvitationEmailBody(string? clientFirstName, string agentName, string invitationLink, string applicationName)
    {
        var greeting = string.IsNullOrWhiteSpace(clientFirstName) ? "Hello," : $"Hello {clientFirstName},";

        return $@"{greeting}
You've been invited by {agentName} to work with them on {applicationName}. We're excited to get you back and help with your listing needs!

To accept this invitation and link to {agentName}, please click the link below:

{invitationLink}

This invitation will expire in 7 days.

If you have any questions, please don't hesitate to reach out to {agentName}.

Best regards,
The {applicationName} Team";
    }

    private static string LoadTemplate(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"RealtorApp.Domain.EmailTemplates.{templateName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException($"Email template '{templateName}' not found as embedded resource.");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string GenerateNewClientInvitationEmailHtml(string? clientFirstName, string agentName, string invitationLink, string applicationName)
    {
        _newClientTemplate ??= LoadTemplate("new-client-invitation.html");

        var greeting = string.IsNullOrWhiteSpace(clientFirstName) ? "Hello," : $"Hello {clientFirstName},";

        return _newClientTemplate
            .Replace("{{ApplicationName}}", applicationName)
            .Replace("{{Greeting}}", greeting)
            .Replace("{{AgentName}}", agentName)
            .Replace("{{InvitationLink}}", invitationLink);
    }

    private static string GenerateExistingClientInvitationEmailHtml(string? clientFirstName, string agentName, string invitationLink, string applicationName)
    {
        _existingClientTemplate ??= LoadTemplate("existing-client-invitation.html");

        var greeting = string.IsNullOrWhiteSpace(clientFirstName) ? "Hello," : $"Hello {clientFirstName},";

        return _existingClientTemplate
            .Replace("{{ApplicationName}}", applicationName)
            .Replace("{{Greeting}}", greeting)
            .Replace("{{AgentName}}", agentName)
            .Replace("{{InvitationLink}}", invitationLink);
    }
}
