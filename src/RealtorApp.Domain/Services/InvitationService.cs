using Microsoft.EntityFrameworkCore;
using RealtorApp.Contracts.Commands.Invitations;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Extensions;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Models;
using Task = System.Threading.Tasks.Task;

namespace RealtorApp.Domain.Services;

public class InvitationService(
    RealtorAppDbContext dbContext,
    IEmailService emailService,
    IUserService userService,
    IAuthProviderService authProviderService,
    ICryptoService crypto,
    IJwtService jwtService,
    IRefreshTokenService refreshTokenService) : IInvitationService
{
    private readonly RealtorAppDbContext _dbContext = dbContext;
    private readonly IEmailService _emailService = emailService;
    private readonly IUserService _userService = userService;
    private readonly IAuthProviderService _authProviderService = authProviderService;
    private readonly ICryptoService _crypto = crypto;
    private readonly IJwtService _jwtService = jwtService;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;

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
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == clientRequest.Email);

                var clientInvitation = new ClientInvitation
                {
                    ClientEmail = clientRequest.Email,
                    ClientFirstName = clientRequest.FirstName,
                    ClientLastName = clientRequest.LastName,
                    ClientPhone = clientRequest.Phone,
                    InvitationToken = Guid.NewGuid(),
                    InvitedBy = agentUserId,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    ClientInvitationsProperties = [.. propertyInvites.Select(i => new ClientInvitationsProperty()
                    {
                        PropertyInvitation = i
                    })]
                };

                var encryptedData = _getEncryptedInviteData(clientInvitation.InvitationToken, existingUser is not null);

                invitesToSend.Add(clientInvitation.ToEmailDto(agentName, encryptedData, existingUser is not null));

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
        var invitation = await _clientInvitationWithPropertiesQuery(invitationToken)
            .AsNoTracking()
            .FirstOrDefaultAsync();


        if (!invitation.IsValid())
        {
            return new ValidateInvitationResponse
            {
                IsValid = false,
                ErrorMessage = "Invalid invitation token"
            };
        }

        return invitation!.ToValidateInvitationResponse();
    }

    public async Task<AcceptInvitationCommandResponse> AcceptInvitationAsync(AcceptInvitationCommand command)
    {
        var clientInvitation = await _clientInvitationWithPropertiesQuery(command.InvitationToken)
                    .FirstOrDefaultAsync(i => i.InvitationToken == command.InvitationToken &&
                                                i.AcceptedAt == null);

        if (!clientInvitation.IsValid())
        {
            return new()
            {
                ErrorMessage = "Invalid invite"
            };
        }

        var authUserDto = await _authProviderService.ValidateTokenAsync(command.FirebaseToken);

        if (authUserDto == null || authUserDto.Email != clientInvitation!.ClientEmail) // second case should never happen, but just incase...
        {
            return new()
            {
                ErrorMessage = "Invalid invite"
            };
        }

        var clientUser = await _dbContext.Clients
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.User.Uuid == Guid.Parse(authUserDto.Uid));

        var propertiesToAdd = clientInvitation.ClientInvitationsProperties.Select(i => i.PropertyInvitation);

        if (clientUser == null) // create client if htey dont exist
        {
            clientUser = clientInvitation.ToClientUser(authUserDto.Uid);
            _dbContext.Clients.Add(clientUser);
        }
        else
        {
            var propertyAddressesMap = clientInvitation.ClientInvitationsProperties
                .ToDictionary(i => i.PropertyInvitation.AddressLine1.ToLower(), i => i.PropertyInvitation);
            propertiesToAdd = await _checkIfPropertiesExistOnExistingUser(clientUser, clientInvitation.InvitedBy, propertyAddressesMap);
        }

        foreach (var propertyToAdd in propertiesToAdd)
        {
            var listing = new Listing()
            {
                Property = propertyToAdd.ToProperty(),
                Conversation = new(),
            };

            var agentListing = new AgentsListing()
            {
                AgentId = clientInvitation.InvitedBy,
            };

            var clientListing = new ClientsListing();

            if (clientUser.UserId == 0)
            {
                clientListing.Client = clientUser;
            }
            else
            {
                clientListing.ClientId = clientUser.UserId;
            }

            listing.AgentsListings.Add(agentListing);
            listing.ClientsListings.Add(clientListing);
            propertyToAdd.CreatedListing = listing;

            await _dbContext.Listings.AddAsync(listing);

        }

        clientInvitation.AcceptedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        var accessToken = _jwtService.GenerateAccessToken(Guid.Parse(authUserDto.Uid), "Client");
        var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(clientUser.UserId);

        return new()
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    private string _getEncryptedInviteData(Guid inviteToken, bool isExistingUser)
    {
        return _crypto.Encrypt($"token={inviteToken}&isExistingUser={isExistingUser}");
    }

    private async Task<List<PropertyInvitation>> _checkIfPropertiesExistOnExistingUser(Client client, long agentId, Dictionary<string, PropertyInvitation> invitedPropertyAddressesMap)
    {
        var propertyInvitationsRemapped = new Dictionary<string, PropertyInvitation>();

        // if even 1 user from a group accepts a new invite, hte other users lose their listing
        var existingProperties = await _dbContext.ClientsListings
            .Include(i => i.Listing)
                .ThenInclude(i => i.Property)
            .Where(i => i.ClientId == client.UserId)
            .ToListAsync();

        foreach (var clientListing in existingProperties)
        {
            var normalizedAddress = clientListing.Listing.Property.AddressLine1.ToLower();
            if (invitedPropertyAddressesMap.TryGetValue(normalizedAddress, out var propertyInvitation))
            {
                var allPeopleAssociatedToListing = await _dbContext.Listings
                    .Include(i => i.AgentsListings)
                    .Include(i => i.ClientsListings)
                    .FirstAsync(i => i.ListingId == clientListing.ListingId);

                _markAllAssociatedListingsAsDeleted(allPeopleAssociatedToListing);

                var listing = new Listing()
                {
                    Conversation = new(),
                    PropertyId = clientListing.Listing.PropertyId
                };

                var newClientListing = new ClientsListing()
                {
                    ClientId = client.UserId,
                };

                var agentListing = new AgentsListing()
                {
                    AgentId = agentId,
                };
                
                listing.ClientsListings.Add(newClientListing);
                listing.AgentsListings.Add(agentListing);
                
                propertyInvitation.CreatedListing = listing;

                await _dbContext.Listings.AddAsync(listing);


                propertyInvitationsRemapped.Add(normalizedAddress, propertyInvitation);
            }
        }

        return [.. invitedPropertyAddressesMap.Except(propertyInvitationsRemapped).Select(i => i.Value)];
    }

    public async Task<ResendInvitationCommandResponse> ResendInvitationAsync(ResendInvitationCommand command, long agentUserId)
    {
        try
        {
            var clientInvitation = await _dbContext.ClientInvitations
                .Include(i => i.InvitedByNavigation)
                .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(i => i.ClientInvitationId == command.ClientInvitationId &&
                                        i.AcceptedAt == null &&
                                        i.DeletedAt == null &&
                                        i.InvitedBy == agentUserId);

            if (clientInvitation == null)
            {
                return new ResendInvitationCommandResponse
                {
                    Success = false,
                    ErrorMessage = "Invitation not found or already accepted"
                };
            }

            // Update client details
            clientInvitation.ClientEmail = command.ClientDetails.Email;
            clientInvitation.ClientFirstName = command.ClientDetails.FirstName;
            clientInvitation.ClientLastName = command.ClientDetails.LastName;
            clientInvitation.ClientPhone = command.ClientDetails.Phone;

            // Generate new token and extend expiry
            clientInvitation.InvitationToken = Guid.NewGuid();
            clientInvitation.ExpiresAt = DateTime.UtcNow.AddDays(7);
            clientInvitation.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            // Check if user already exists for email type determination
            var existingUser = await _userService.GetUserByEmailAsync(command.ClientDetails.Email);

            // Get agent name for email
            var agentName = $"{clientInvitation.InvitedByNavigation.User.FirstName} {clientInvitation.InvitedByNavigation.User.LastName}".Trim();

            // Generate encrypted invitation data
            var encryptedData = _getEncryptedInviteData(clientInvitation.InvitationToken, existingUser != null);

            // Create email DTO and send
            var emailDto = clientInvitation.ToEmailDto(agentName, encryptedData, existingUser != null);
            var failedInvites = await _emailService.SendBulkInvitationEmailsAsync([emailDto]);

            if (failedInvites.Count > 0)
            {
                return new ResendInvitationCommandResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to send invitation email"
                };
            }

            return new ResendInvitationCommandResponse
            {
                Success = true
            };
        }
        catch (Exception)
        {
            return new ResendInvitationCommandResponse
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred"
            };
        }
    }

    private void _markAllAssociatedListingsAsDeleted(Listing listing)
    {
        listing.DeletedAt = DateTime.UtcNow;

        if (listing.Conversation != null)
        {
            listing.Conversation.DeletedAt = DateTime.UtcNow;
        }

        foreach (var agentListing in listing.AgentsListings)
        {
            agentListing.DeletedAt = DateTime.UtcNow;
        }

        foreach (var clientListing in listing.ClientsListings)
        {
            clientListing.DeletedAt = DateTime.UtcNow;
        }
    }

    private IQueryable<ClientInvitation> _clientInvitationWithPropertiesQuery(Guid invitationToken)
    {
        return _dbContext.ClientInvitations
            .Include(i => i.ClientInvitationsProperties)
                .ThenInclude(cip => cip.PropertyInvitation)
            .Where(i => i.InvitationToken == invitationToken &&
                    i.AcceptedAt == null);
    }
}