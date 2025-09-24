using Microsoft.EntityFrameworkCore;
using RealtorApp.Contracts.Commands.Invitations;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Extensions;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Models;
using Task = System.Threading.Tasks.Task;

namespace RealtorApp.Domain.Services;

public class InvitationService(RealtorAppDbContext dbContext, IEmailService emailService, IUserService userService) : IInvitationService
{
    private readonly RealtorAppDbContext _dbContext = dbContext;
    private readonly IEmailService _emailService = emailService;
    private readonly IUserService _userService = userService;

    public async Task<SendInvitationCommandResponse> SendInvitationsAsync(SendInvitationCommand command, long agentUserId)
    {
        var agentName = await _userService.GetAgentName(agentUserId);

        if (agentName == null)
        {
            return new()
            {
                Errors = ["agent not found"]
            };
        }

        var response = new SendInvitationCommandResponse();
        var propertyInvites = new List<PropertyInvitation>();
        var invitesToSend = new List<InvitationEmailDto>();
        try
        {
            foreach (var propertyRequest in command.Properties)
            {
                var property = new PropertyInvitation
                {
                    AddressLine1 = propertyRequest.AddressLine1,
                    AddressLine2 = propertyRequest.AddressLine2,
                    City = propertyRequest.City,
                    Region = propertyRequest.Region,
                    PostalCode = propertyRequest.PostalCode,
                    CountryCode = propertyRequest.CountryCode,
                    InvitedBy = agentUserId
                };

                propertyInvites.Add(property);
            }

            // Create clients and process invitations
            foreach (var clientRequest in command.Clients)
            {
                var existingUser = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == clientRequest.Email && u.DeletedAt == null);

                var clientInvitation = new ClientInvitation
                {
                    ClientEmail = clientRequest.Email,
                    ClientFirstName = clientRequest.FirstName,
                    ClientLastName = clientRequest.LastName,
                    ClientPhone = clientRequest.Phone,
                    InvitedBy = agentUserId,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    ClientInvitationsProperties = [.. propertyInvites.Select(i => new ClientInvitationsProperty()
                    {
                        PropertyInvitation = i
                    })]
                };

                invitesToSend.Add(clientInvitation.ToEmailDto(agentName, existingUser is not null));

                _dbContext.ClientInvitations.Add(clientInvitation);
            }

            await _dbContext.SaveChangesAsync();

            var failedInvites = await _emailService.SendBulkInvitationEmailsAsync(invitesToSend);

            if (failedInvites.Count > 0)
            {
                response.Errors = [.. failedInvites.Select(i => $"Failed to send invite to {i.ClientFirstName}")];
            }

            response.InvitationsSent = invitesToSend.Count - failedInvites.Count;

            return response;
        }
        catch (Exception)
        {
            response.Errors.Add($"An unexpected error occurred");
            return response;
        }
    }

    public async Task<ValidateInvitationResponse> ValidateInvitationAsync(Guid invitationToken)
    {
        var invitation = await _dbContext.ClientInvitations
            .FirstOrDefaultAsync(i => i.InvitationToken == invitationToken &&
                                      i.DeletedAt == null &&
                                      i.AcceptedAt == null);

        if (invitation == null)
        {
            return new ValidateInvitationResponse
            {
                IsValid = false,
                ErrorMessage = "Invalid invitation token"
            };
        }

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            return new ValidateInvitationResponse
            {
                IsValid = false,
                ErrorMessage = "Invitation has expired"
            };
        }

        return new ValidateInvitationResponse
        {
            IsValid = true,
            ClientEmail = invitation.ClientEmail,
            ClientFirstName = invitation.ClientFirstName,
            ClientLastName = invitation.ClientLastName,
            ClientPhone = invitation.ClientPhone,
            ExpiresAt = invitation.ExpiresAt
        };
    }

    public async Task<AcceptInvitationCommandResponse> AcceptInvitationAsync(AcceptInvitationCommand command)
    {
        // TODO: Implement invitation acceptance logic
        // This will involve:
        // 1. Validating the invitation token
        // 2. Validating the Firebase token
        // 3. Updating the user record with the Firebase UID
        // 4. Marking the invitation as accepted
        // 5. Generating JWT tokens for the user

        //TODO:
        // client already exists
        // // dont create user,
        // // send a different type of invite
        // property already exists
        // // if the client doesnt exist on the property, create the relationship
        // // if they do, reassign the client
        await Task.CompletedTask;
        throw new NotImplementedException("AcceptInvitationAsync will be implemented after email service");
    }
}