using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Settings;

namespace RealtorApp.Domain.Services;

public class EmailService(IOptions<AppSettings> appSettings, ILogger<EmailService> logger, ICryptoService crypto) : IEmailService
{
    private readonly AppSettings _appSettings = appSettings.Value;
    private readonly ILogger<EmailService> _logger = logger;
    private readonly ICryptoService _crypto = crypto;

    public async Task<bool> SendInvitationEmailAsync(string clientEmail, string? clientFirstName, Guid invitationToken, string agentName, bool isExistingUser)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_appSettings.Email.Smtp.FromName, _appSettings.Email.Smtp.FromEmail));
            message.To.Add(new MailboxAddress(clientFirstName ?? clientEmail, clientEmail));
            message.Subject = $"You've been invited to {_appSettings.ApplicationName} by {agentName}";

            var invitationLink = GenerateInvitationLink(invitationToken, isExistingUser);
            var emailBody = GenerateNewClientInvitationEmailBody(clientFirstName, agentName, invitationLink, _appSettings.ApplicationName);

            //TODO: change this to html
            message.Body = new TextPart("plain")
            {
                Text = emailBody
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_appSettings.Email.Smtp.Host, _appSettings.Email.Smtp.Port,
                _appSettings.Email.Smtp.EnableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);

            if (!string.IsNullOrEmpty(_appSettings.Email.Smtp.Username))
            {
                await client.AuthenticateAsync(_appSettings.Email.Smtp.Username, _appSettings.Email.Smtp.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

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

        foreach (var invitation in invitations)
        {
            var success = await SendInvitationEmailAsync(
                invitation.ClientEmail,
                invitation.ClientFirstName,
                invitation.InvitationToken,
                invitation.AgentName,
                invitation.IsExistingUser
            );

            if (!success)
            {
                failedInvites.Add(invitation);
            }
        }

        return failedInvites;
    }

    private string GenerateInvitationLink(Guid invitationToken, bool isExistingUser)
    {
        var encryptedData = _crypto.Encrypt($"token={invitationToken}&isExistingUser={isExistingUser}");
        return $"{_appSettings.FrontendBaseUrl}/invitations/accept?data={encryptedData}";
    }

    private static string GenerateNewClientInvitationEmailBody(string? clientFirstName, string agentName, string invitationLink, string applicationName)
    {
        var greeting = string.IsNullOrWhiteSpace(clientFirstName) ? "Hello," : $"Hello {clientFirstName},";

        return $@"{greeting}

        You've been invited by {agentName} to join {applicationName}. We're excited to help you with your property needs!

        To accept this invitation and create your account, please click the link below:
        {invitationLink}

        This invitation will expire in 7 days.

        If you have any questions, please don't hesitate to reach out to {agentName}.

        Best regards,
        The {applicationName} Team";
    }
}