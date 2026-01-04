using System.Reflection;
using System.Web;
using Amazon;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Logging;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Settings;
using RealtorApp.Infra.Data;

namespace RealtorApp.Domain.Services;

public class EmailService(AppSettings appSettings, ILogger<EmailService> logger) : IEmailService
{
    private readonly AppSettings _appSettings = appSettings;
    private readonly ILogger<EmailService> _logger = logger;
    private static string? _newClientTemplate;
    private static string? _existingClientTemplate;
    private static string? _newTeammateTemplate;
    private static string? _existingTeammateTemplate;

    public async Task<bool> SendClientInvitationEmailAsync(string clientEmail, string? clientFirstName, string agentName, string encryptedData, bool isExistingUser)
    {
        try
        {
            var invitationLink = GenerateInvitationLink(encryptedData);
            var emailBody = isExistingUser
                    ? GenerateExistingClientInvitationEmailBody(clientFirstName, agentName, invitationLink)
                    : GenerateNewClientInvitationEmailBody(clientFirstName, agentName, invitationLink);
            var credentials = new Amazon.Runtime.BasicAWSCredentials(
                _appSettings.Aws.AccessKey,
                _appSettings.Aws.SecretKey
            );

            var region = RegionEndpoint.GetBySystemName(_appSettings.Aws.Ses.Region);
            using var client = new AmazonSimpleEmailServiceV2Client(credentials, region);

            var htmlBody = isExistingUser
                ? GenerateExistingClientInvitationEmailHtml(clientFirstName, agentName, invitationLink)
                : GenerateNewClientInvitationEmailHtml(clientFirstName, agentName, invitationLink);

            var sendRequest = new SendEmailRequest
            {
                FromEmailAddress = $"{_appSettings.Aws.Ses.FromName} <{_appSettings.Aws.Ses.FromEmail}>",
                Destination = new Destination
                {
                    ToAddresses = [clientEmail]
                },
                Content = new EmailContent
                {
                    Simple = new Amazon.SimpleEmailV2.Model.Message
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

    public async Task<List<InvitationEmailDto>> SendClientBulkInvitationEmailsAsync(List<InvitationEmailDto> invitations)
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
                    ? GenerateExistingClientInvitationEmailBody(invitation.ClientFirstName, invitation.AgentName, invitationLink)
                    : GenerateNewClientInvitationEmailBody(invitation.ClientFirstName, invitation.AgentName, invitationLink);

                var htmlBody = invitation.IsExistingUser
                    ? GenerateExistingClientInvitationEmailHtml(invitation.ClientFirstName, invitation.AgentName, invitationLink)
                    : GenerateNewClientInvitationEmailHtml(invitation.ClientFirstName, invitation.AgentName, invitationLink);

                var sendRequest = new SendEmailRequest
                {
                    FromEmailAddress = $"{_appSettings.Aws.Ses.FromName} <{_appSettings.Aws.Ses.FromEmail}>",
                    Destination = new Destination
                    {
                        ToAddresses = [invitation.ClientEmail]
                    },
                    Content = new EmailContent
                    {
                        Simple = new Amazon.SimpleEmailV2.Model.Message
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

    public async Task<int> SendTeammateBulkInvitationEmailsAsync(TeammateInvitationEmailDto[] teammateInvitations)
    {
        var sentCount = 0;

        var credentials = new Amazon.Runtime.BasicAWSCredentials(
            _appSettings.Aws.AccessKey,
            _appSettings.Aws.SecretKey
        );

        var region = RegionEndpoint.GetBySystemName(_appSettings.Aws.Ses.Region);
        using var client = new AmazonSimpleEmailServiceV2Client(credentials, region);

        foreach (var invitation in teammateInvitations)
        {
            try
            {
                var invitationLink = GenerateInvitationLink(invitation.EncryptedData);
                var emailBody = invitation.IsExistingUser
                    ? GenerateExistingTeammateInvitationEmailBody(invitation.TeammateFirstName, invitation.AgentName, invitationLink)
                    : GenerateNewTeammateInvitationEmailBody(invitation.TeammateFirstName, invitation.AgentName, invitationLink);

                var htmlBody = invitation.IsExistingUser
                    ? GenerateExistingTeammateInvitationEmailHtml(invitation.TeammateFirstName, invitation.AgentName, invitationLink)
                    : GenerateNewTeammateInvitationEmailHtml(invitation.TeammateFirstName, invitation.AgentName, invitationLink);

                var sendRequest = new SendEmailRequest
                {
                    FromEmailAddress = $"{_appSettings.Aws.Ses.FromName} <{_appSettings.Aws.Ses.FromEmail}>",
                    Destination = new Destination
                    {
                        ToAddresses = [invitation.TeammateEmail]
                    },
                    Content = new EmailContent
                    {
                        Simple = new Amazon.SimpleEmailV2.Model.Message
                        {
                            Subject = new Content
                            {
                                Data = $"You've been invited to collaborate with {invitation.AgentName} on {_appSettings.AppName}",
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

                _logger.LogInformation("Successfully sent teammate invitation email to {TeammateEmail} from {AgentName}",
                    invitation.TeammateEmail, invitation.AgentName);
                sentCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send teammate invitation email to {TeammateEmail}", invitation.TeammateEmail);
            }
        }

        return sentCount;
    }

    private string GenerateNewTeammateInvitationEmailBody(string? teammateFirstName, string agentName, string invitationLink)
    {
        var greeting = string.IsNullOrWhiteSpace(teammateFirstName) ? "Hello," : $"Hello {teammateFirstName},";

        return $@"{greeting}

You've been invited by {agentName} to collaborate on a listing on {_appSettings.AppName}. Join the team and help close this deal!

To accept this invitation and create your account, please click the link below:

{invitationLink}

This invitation will expire in {_appSettings.TeammateInvitationExpirationDays} days.

If you have any questions, please don't hesitate to reach out to {agentName}.

Best regards,
The {_appSettings.AppName} Team";
    }

    private string GenerateExistingTeammateInvitationEmailBody(string? teammateFirstName, string agentName, string invitationLink)
    {
        var greeting = string.IsNullOrWhiteSpace(teammateFirstName) ? "Hello," : $"Hello {teammateFirstName},";

        return $@"{greeting}

You've been invited by {agentName} to collaborate on a listing on {_appSettings.AppName}. Join the team and help close this deal!

To accept this invitation and start collaborating with {agentName}, please click the link below:

{invitationLink}

This invitation will expire in {_appSettings.TeammateInvitationExpirationDays} days.

If you have any questions, please don't hesitate to reach out to {agentName}.

Best regards,
The {_appSettings.AppName} Team";
    }
    private string GenerateInvitationLink(string encryptedData)
    {
        return $"{_appSettings.ClientUrl}/invitations/accept?data={HttpUtility.UrlEncode(encryptedData)}";
    }

    private string GenerateNewClientInvitationEmailBody(string? clientFirstName, string agentName, string invitationLink)
    {
        var greeting = string.IsNullOrWhiteSpace(clientFirstName) ? "Hello," : $"Hello {clientFirstName},";

        return $@"{greeting}

You've been invited by {agentName} to join {_appSettings.AppName}. We're excited to help you with your listing needs!

To accept this invitation and create your account, please click the link below:

{invitationLink}

This invitation will expire in {_appSettings.ClientInvitationExpirationDays} days.

If you have any questions, please don't hesitate to reach out to {agentName}.

Best regards,
The {_appSettings.AppName} Team";
    }

    private string GenerateExistingClientInvitationEmailBody(string? clientFirstName, string agentName, string invitationLink)
    {
        var greeting = string.IsNullOrWhiteSpace(clientFirstName) ? "Hello," : $"Hello {clientFirstName},";

        return $@"{greeting}
You've been invited by {agentName} to work with them on {_appSettings.AppName}. We're excited to get you back and help with your listing needs!

To accept this invitation and link to {agentName}, please click the link below:

{invitationLink}

This invitation will expire in {_appSettings.ClientInvitationExpirationDays} days.

If you have any questions, please don't hesitate to reach out to {agentName}.

Best regards,
The {_appSettings.AppName} Team";
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

    private string GenerateNewClientInvitationEmailHtml(string? clientFirstName, string agentName, string invitationLink)
    {
        _newClientTemplate ??= LoadTemplate("new-client-invitation.html");

        var greeting = string.IsNullOrWhiteSpace(clientFirstName) ? "Hello," : $"Hello {clientFirstName},";

        return _newClientTemplate
            .Replace("{{ApplicationName}}", _appSettings.AppName)
            .Replace("{{Greeting}}", greeting)
            .Replace("{{AgentName}}", agentName)
            .Replace("{{InvitationLink}}", invitationLink)
            .Replace("{{ExpirationDays}}", _appSettings.ClientInvitationExpirationDays.ToString());
    }

    private string GenerateExistingClientInvitationEmailHtml(string? clientFirstName, string agentName, string invitationLink)
    {
        _existingClientTemplate ??= LoadTemplate("existing-client-invitation.html");

        var greeting = string.IsNullOrWhiteSpace(clientFirstName) ? "Hello," : $"Hello {clientFirstName},";

        return _existingClientTemplate
            .Replace("{{ApplicationName}}", _appSettings.AppName)
            .Replace("{{Greeting}}", greeting)
            .Replace("{{AgentName}}", agentName)
            .Replace("{{InvitationLink}}", invitationLink)
            .Replace("{{ExpirationDays}}", _appSettings.ClientInvitationExpirationDays.ToString());
    }

    private string GenerateNewTeammateInvitationEmailHtml(string? teammateFirstName, string agentName, string invitationLink)
    {
        _newTeammateTemplate ??= LoadTemplate("new-teammate-invitation.html");

        var greeting = string.IsNullOrWhiteSpace(teammateFirstName) ? "Hello," : $"Hello {teammateFirstName},";

        return _newTeammateTemplate
            .Replace("{{ApplicationName}}", _appSettings.AppName)
            .Replace("{{Greeting}}", greeting)
            .Replace("{{AgentName}}", agentName)
            .Replace("{{InvitationLink}}", invitationLink)
            .Replace("{{ExpirationDays}}", _appSettings.TeammateInvitationExpirationDays.ToString());
    }

    private string GenerateExistingTeammateInvitationEmailHtml(string? teammateFirstName, string agentName, string invitationLink)
    {
        _existingTeammateTemplate ??= LoadTemplate("existing-teammate-invitation.html");

        var greeting = string.IsNullOrWhiteSpace(teammateFirstName) ? "Hello," : $"Hello {teammateFirstName},";

        return _existingTeammateTemplate
            .Replace("{{ApplicationName}}", _appSettings.AppName)
            .Replace("{{Greeting}}", greeting)
            .Replace("{{AgentName}}", agentName)
            .Replace("{{InvitationLink}}", invitationLink)
            .Replace("{{ExpirationDays}}", _appSettings.TeammateInvitationExpirationDays.ToString());
    }
}
