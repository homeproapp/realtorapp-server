using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealtorApp.Contracts.Commands.Invitations.Requests;
using RealtorApp.Contracts.Commands.Invitations.Responses;
using RealtorApp.Contracts.Enums;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Extensions;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Settings;
using RealtorApp.Infra.Data;

namespace RealtorApp.Domain.Services;

public class InvitationService(
    RealtorAppDbContext dbContext,
    IEmailService emailService,
    IUserService userService,
    IUserAuthService userAuthService,
    IAuthProviderService authProviderService,
    ICryptoService crypto,
    IJwtService jwtService,
    IRefreshTokenService refreshTokenService,
    AppSettings settings,
    ILogger<InvitationService> logger) : IInvitationService
{
    private readonly RealtorAppDbContext _dbContext = dbContext;
    private readonly IEmailService _emailService = emailService;
    private readonly IUserService _userService = userService;
    private readonly IUserAuthService _userAuthService = userAuthService;
    private readonly IAuthProviderService _authProviderService = authProviderService;
    private readonly ICryptoService _crypto = crypto;
    private readonly IJwtService _jwtService = jwtService;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
    private readonly AppSettings _settings = settings;
    private readonly ILogger<InvitationService> _logger = logger;

    public async Task<SendTeammateInvitationCommandResponse> SendTeammateInvitationsAsync(SendTeammateInvitationCommand command, long invitingAgentId)
    {
        var teammateInvitations = new List<TeammateInvitation>();
        var teammateEmails = command.Teammates.Select(i => i.Email.ToLower());

        var existingUsersByEmailToId = await _dbContext.Users
            .AsNoTracking()
            .Where(i => teammateEmails.Contains(i.Email.ToLower()))
            .Select(i => new { Email = i.Email.ToLower(), i.UserId })
            .ToDictionaryAsync(i => i.Email, i => i.UserId);

        var existingInvitesByEmail = await _dbContext.TeammateInvitations
            .Where(i => i.AcceptedAt == null &&
                i.DeletedAt == null &&
                i.InvitedBy == invitingAgentId &&
                i.InvitedListingId == command.ListingId &&
                teammateEmails.Contains(i.TeammateEmail.ToLower()))
            .ToDictionaryAsync(i => i.TeammateEmail.ToLower(), i => i);

        foreach (var teammate in command.Teammates)
        {
            _ = existingInvitesByEmail.TryGetValue(teammate.Email.ToLower(), out TeammateInvitation? existingInvite);

            if (existingInvite != null && existingInvite.AcceptedAt != null)
            {
                // user is being reinvited to the same listing after accepting
                _logger.LogWarning("User was reinvited to listing they already accepted {InvitedBy} - {UserInvited} - {ListingId}",
                    invitingAgentId, existingInvite.CreatedUserId, command.ListingId);
                continue;
            }

            // not sure this is best way.. but can work for now.
            if (existingInvite != null &&
                ((DateTime.UtcNow - existingInvite.CreatedAt) >= TimeSpan.FromMinutes(_settings.MinimumWaitInvitationMinutes)))
            {
                // mark existing as deleted, and allow it to send a new one if the user hasnt accepted
                //TODO: we will have to implement some way to limit this, so users arent getting spammed.
                existingInvite.DeletedAt = DateTime.UtcNow;
            }

            var teammateInvitation = teammate.ToTeammateInvitation(command.ListingId, invitingAgentId);
            teammateInvitation.ExpiresAt = DateTime.UtcNow.AddDaysAndSetToEndOfDay(_settings.TeammateInvitationExpirationDays);
            // we dont assign createduserId here even if they arleady exist
            // leaving that for accept invitation flow.
            teammateInvitations.Add(teammateInvitation);
        }

        await _dbContext.TeammateInvitations.AddRangeAsync(teammateInvitations);

        await _dbContext.SaveChangesAsync();

        var invitingUser = await _dbContext.Users
            .Where(i => i.UserId == invitingAgentId)
            .Select(i => new
            {
                i.FirstName,
                i.LastName
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (invitingUser == null)
        {
            return new() { ErrorMessage = "Invting user not found" };
        }

        var agentName = invitingUser.FirstName + " " + invitingUser.LastName;

        var dtos = teammateInvitations
            .Select(i => {
                bool isExisting = existingUsersByEmailToId.TryGetValue(i.TeammateEmail.ToLower(), out long id);
                return i.ToDto(_getEncryptedInviteData(i.InvitationToken, isExisting), agentName, isExisting);
            })
            .ToArray();

        var sentCount = await _emailService.SendTeammateBulkInvitationEmailsAsync(dtos);

        return new() { InvitationsSent = sentCount };
    }

    public async Task<SendInvitationCommandResponse> SendClientInvitationsAsync(SendInvitationCommand command, long agentUserId)
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
        var invitesToSend = new List<(ClientInvitation Client, bool IsExistingUser)>();
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
            // TODO: can we do this in a batch operation, always spooky seeing dbcontext in a loop
            //// I anticpate, most invites will contain a few clients at most.
            foreach (var clientRequest in command.Clients)
            {
                // check if agent has already invited this client and the client hasnt accepted yet
                var clientInvitation = await _dbContext.ClientInvitations
                    .Include(i => i.ClientInvitationsProperties)
                        .ThenInclude(i => i.PropertyInvitation)
                    .FirstOrDefaultAsync(i => i.InvitedBy == agentUserId &&
                    EF.Functions.ILike(i.ClientEmail, clientRequest.Email) &&
                    i.AcceptedAt == null);

                if (clientInvitation != null)
                {
                    //re-up the expiration if it's an existing clientInvitation
                    clientInvitation.ExpiresAt = DateTime.UtcNow.AddDaysAndSetToEndOfDay(_settings.ClientInvitationExpirationDays);
                    var existingProperties = clientInvitation.ClientInvitationsProperties.Select(i => i.PropertyInvitation.AddressLine1.ToLower());
                    var netNewProperties = propertyInvites
                        .Where(i =>  !existingProperties.Contains(i.AddressLine1.ToLower()));

                    var clientInvitationProperties = netNewProperties.Select(i => new ClientInvitationsProperty()
                    {
                       PropertyInvitation = i,
                       ClientInvitationId = clientInvitation.ClientInvitationId
                    });

                    await _dbContext.ClientInvitationsProperties.AddRangeAsync(clientInvitationProperties);
                }

                User? existingUser = null;

                // user may exist in system since invite could have been accepted already
                if (clientInvitation == null)
                {
                    existingUser = await _dbContext.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Email == clientRequest.Email);
                }

                if (clientInvitation == null)
                {
                    clientInvitation = new ClientInvitation
                    {
                        ClientEmail = clientRequest.Email,
                        ClientFirstName = clientRequest.FirstName,
                        ClientLastName = clientRequest.LastName,
                        ClientPhone = clientRequest.Phone,
                        InvitationToken = Guid.NewGuid(),
                        InvitedBy = agentUserId,
                        ExpiresAt = DateTime.UtcNow.AddDaysAndSetToEndOfDay(_settings.ClientInvitationExpirationDays),
                        ClientInvitationsProperties = [.. propertyInvites.Select(i => new ClientInvitationsProperty()
                        {
                            PropertyInvitation = i
                        })]
                    };

                    _dbContext.ClientInvitations.Add(clientInvitation);
                }

                invitesToSend.Add((clientInvitation, existingUser is not null));

            }

            await _dbContext.SaveChangesAsync();

            var failedInvites = await _emailService.SendClientBulkInvitationEmailsAsync([.. invitesToSend
                .Select(i =>
                    i.Client.ToEmailDto(agentName, _getEncryptedInviteData(i.Client.InvitationToken, i.IsExistingUser), i.IsExistingUser))]);

            if (failedInvites.Count > 0)
            {
                response.Errors = [.. failedInvites.Select(i => $"Failed to send invite to {i.ClientFirstName}")];
            }

            response.InvitationsSent = invitesToSend.Count - failedInvites.Count;

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong sending invite Message = {Message}", ex.Message);
            response.Errors.Add($"An unexpected error occurred");
            return response;
        }
    }

    public async Task<ValidateTeammateInvitationResponse> ValidateTeammateInvitationAsync(Guid invitationToken)
    {
        var invitation = await _dbContext.TeammateInvitations
            .Include(i => i.InvitedListing)
            .Where(i => i.InvitationToken == invitationToken)
            .AsNoTracking()
            .FirstOrDefaultAsync();


        if (!invitation.IsValid())
        {
            return new ValidateTeammateInvitationResponse
            {
                IsValid = false,
                ErrorMessage = "Invalid invitation"
            };
        }

        return invitation!.ToValidateInvitationResponse();
    }

    public async Task<ValidateInvitationResponse> ValidateClientInvitationAsync(Guid invitationToken)
    {
        var invitation = await _clientInvitationWithPropertiesQuery(invitationToken)
            .AsNoTracking()
            .FirstOrDefaultAsync();


        if (!invitation.IsValid())
        {
            return new ValidateInvitationResponse
            {
                IsValid = false,
                ErrorMessage = "Invalid invitation"
            };
        }

        return invitation!.ToValidateInvitationResponse();
    }

    public async Task<AcceptInvitationCommandResponse> AcceptTeammateInvitationAsync(AcceptInvitationCommand command)
    {
        var tokenFromEncryptedData = GetTokenFromEncryptedData(command.InvitationToken);
        var teammateInvitation = await _dbContext.TeammateInvitations
            .Where(i => i.InvitationToken == tokenFromEncryptedData &&
                i.AcceptedAt == null)
            .FirstOrDefaultAsync();

        if (!teammateInvitation.IsValid())
        {
            return new()
            {
                ErrorMessage = "Invalid invite"
            };
        }

        var existingUser = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => EF.Functions.ILike(u.Email, teammateInvitation!.TeammateEmail));


        AuthProviderUserDto? authUserDto;
        bool isNewFirebaseUser = false;

        if (existingUser != null)
        {
            var userExistsOnListing = await UserAlreadyExistsOnListing(existingUser.UserId, teammateInvitation!.InvitedListingId, (TeammateTypes)teammateInvitation.TeammateRoleType);

            if (userExistsOnListing)
            {
                _logger.LogError("User tried to accept invite while already on listing - {AcceptingUser} on Listing {ListingId} ", existingUser.UserId, teammateInvitation.InvitedListingId);
                return new() { ErrorMessage = "Invalid invite" };
            }

            authUserDto = await _authProviderService.SignInWithEmailAndPasswordAsync(teammateInvitation!.TeammateEmail, command.Password);
        }
        else
        {
            if (string.IsNullOrEmpty(command.EnteredFirstName) || string.IsNullOrEmpty(command.EnteredLastName))
            {
                return new() { ErrorMessage = "Invalid form data" };
            }
            var displayName = $"{command.EnteredFirstName} {command.EnteredLastName}";
            var firebaseUser = await _authProviderService.RegisterWithEmailAndPasswordAsync(
                teammateInvitation!.TeammateEmail,
                command.Password,
                emailVerified: false
            );

            if (firebaseUser == null)
            {
                return new()
                {
                    ErrorMessage = "Failed to create user account"
                };
            }

            isNewFirebaseUser = true;
            authUserDto = new AuthProviderUserDto
            {
                Uid = firebaseUser.Uid,
                Email = firebaseUser.Email!,
                DisplayName = displayName
            };
        }

        if (authUserDto == null)
        {
            return new()
            {
                ErrorMessage = "Authentication failed"
            };
        }

        var user = existingUser;
        try
        {

            if (user != null)
            {
                teammateInvitation.CreatedUserId = user.UserId;
            } else
            {
                user = teammateInvitation.ToTeammateUserByType(command, authUserDto.Uid);
            }

            if ((TeammateTypes)teammateInvitation.TeammateRoleType == TeammateTypes.Agent)
            {
                var agentListing = new AgentsListing()
                {
                    AgentId = user.UserId,
                    ListingId = teammateInvitation.InvitedListingId
                };

                await _dbContext.AgentsListings.AddAsync(agentListing);
            }

            //TODO: other types to be added
        }
        catch (Exception)
        {
            if (isNewFirebaseUser)
            {
                await _authProviderService.DeleteUserAsync(authUserDto.Uid);
            }
            throw;
        }


        teammateInvitation.AcceptedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _userAuthService.InvalidateUserToListingIdsCache(user.UserId);

        var accessToken = _jwtService.GenerateAccessToken(authUserDto.Uid,
            ((TeammateTypes)teammateInvitation.TeammateRoleType).ToString());
        var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user!.UserId);

        return new()
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

    }

    private async Task<bool> UserAlreadyExistsOnListing(long userId, long listingId, TeammateTypes teammateType)
    {
        if (teammateType == TeammateTypes.Agent)
        {
            return await _dbContext.AgentsListings.AnyAsync(i => i.AgentId == userId && i.ListingId == listingId);
        }

        // true is default case so it will error if for some reason the teammate type isnt covered
        return true;
    }

    public async Task<AcceptInvitationCommandResponse> AcceptClientInvitationAsync(AcceptInvitationCommand command)
    {
        var tokenFromEncryptedData = GetTokenFromEncryptedData(command.InvitationToken);
        var clientInvitation = await _clientInvitationWithPropertiesQuery(tokenFromEncryptedData)
                    .FirstOrDefaultAsync(i => i.InvitationToken == tokenFromEncryptedData &&
                                                i.AcceptedAt == null);

        if (!clientInvitation.IsValid())
        {
            return new()
            {
                ErrorMessage = "Invalid invite"
            };
        }

        var existingUser = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => EF.Functions.ILike(u.Email, clientInvitation!.ClientEmail));

        AuthProviderUserDto? authUserDto;
        bool isNewFirebaseUser = false;

        if (existingUser != null)
        {
            authUserDto = await _authProviderService.SignInWithEmailAndPasswordAsync(clientInvitation!.ClientEmail, command.Password);
        }
        else
        {
            if (string.IsNullOrEmpty(command.EnteredFirstName) || string.IsNullOrEmpty(command.EnteredLastName))
            {
                return new() { ErrorMessage = "Invalid form data" };
            }

            var displayName = $"{command.EnteredFirstName} {command.EnteredFirstName}";
            var firebaseUser = await _authProviderService.RegisterWithEmailAndPasswordAsync(
                clientInvitation!.ClientEmail,
                command.Password,
                emailVerified: false
            );

            if (firebaseUser == null)
            {
                return new()
                {
                    ErrorMessage = "Failed to create user account"
                };
            }

            isNewFirebaseUser = true;
            authUserDto = new AuthProviderUserDto
            {
                Uid = firebaseUser.Uid,
                Email = firebaseUser.Email!,
                DisplayName = displayName
            };
        }

        if (authUserDto == null)
        {
            return new()
            {
                ErrorMessage = "Authentication failed"
            };
        }

        try
        {
            var clientUser = await _dbContext.Clients
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.User.Uuid == authUserDto.Uid);

            var propertiesToAdd = clientInvitation.ClientInvitationsProperties.Select(i => i.PropertyInvitation);

            if (clientUser == null)
            {
                clientUser = clientInvitation.ToClientUser(command, authUserDto.Uid);
                _dbContext.Clients.Add(clientUser);
            }
            else
            {
                var propertyAddressesMap = clientInvitation.ClientInvitationsProperties
                    .ToDictionary(i => i.PropertyInvitation.AddressLine1.ToLower(), i => i.PropertyInvitation);
                propertiesToAdd = await _checkIfPropertiesExistOnExistingUser(clientUser, clientInvitation.InvitedBy, propertyAddressesMap);
            }

            clientInvitation.CreatedUser = clientUser;

            foreach (var propertyToAdd in propertiesToAdd)
            {
                Listing listing;
                if (!propertyToAdd.CreatedListingId.HasValue)
                {
                    listing = new Listing()
                    {
                        Property = propertyToAdd.ToProperty(),
                        Conversation = new(),
                    };
                    await _dbContext.Listings.AddAsync(listing);

                    // lead agent is set when the listing is first being created
                    // this is the first agent to "own" the listing
                    // other agents can be added later, in which case this value will be false.
                    var agentListing = new AgentsListing()
                    {
                        AgentId = clientInvitation.InvitedBy,
                        IsLeadAgent = true
                    };

                    listing.AgentsListings.Add(agentListing);
                    propertyToAdd.CreatedListing = listing;
                }
                else
                {
                    listing = propertyToAdd.CreatedListing!;
                }



                var clientListing = new ClientsListing();

                if (clientUser.UserId == 0)
                {
                    clientListing.Client = clientUser;
                }
                else
                {
                    clientListing.ClientId = clientUser.UserId;
                }

                listing.ClientsListings.Add(clientListing);
            }

            clientInvitation.AcceptedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _userAuthService.InvalidateUserToListingIdsCache(clientInvitation.InvitedBy);

            var accessToken = _jwtService.GenerateAccessToken(authUserDto.Uid, "Client");
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(clientUser.UserId);

            return new()
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
        catch (Exception)
        {
            if (isNewFirebaseUser)
            {
                await _authProviderService.DeleteUserAsync(authUserDto.Uid);
            }
            throw;
        }
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

    public async Task<ResendInvitationCommandResponse> ResendClientInvitationAsync(ResendInvitationCommand command, long agentUserId)
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

            clientInvitation.ClientEmail = command.ClientDetails.Email;
            clientInvitation.ClientFirstName = command.ClientDetails.FirstName;
            clientInvitation.ClientLastName = command.ClientDetails.LastName;
            clientInvitation.ClientPhone = command.ClientDetails.Phone;

            // unique index prevents inserting new records, so we update instead
            clientInvitation.InvitationToken = Guid.NewGuid();
            clientInvitation.ExpiresAt = DateTime.UtcNow.AddDaysAndSetToEndOfDay(_settings.ClientInvitationExpirationDays);
            clientInvitation.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            var existingUser = await _userService.GetUserByEmailAsync(command.ClientDetails.Email);

            var agentName = $"{clientInvitation.InvitedByNavigation.User.FirstName} {clientInvitation.InvitedByNavigation.User.LastName}".Trim();

            var encryptedData = _getEncryptedInviteData(clientInvitation.InvitationToken, existingUser != null);

            var emailDto = clientInvitation.ToEmailDto(agentName, encryptedData, existingUser != null);
            var failedInvites = await _emailService.SendClientBulkInvitationEmailsAsync([emailDto]);

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
            .Include(i => i.InvitedByNavigation)
                .ThenInclude(i => i.User)
            .Include(i => i.ClientInvitationsProperties)
                .ThenInclude(cip => cip.PropertyInvitation)
                    .ThenInclude(i => i.CreatedListing)
            .Where(i => i.InvitationToken == invitationToken &&
                    i.AcceptedAt == null);
    }

    private Guid GetTokenFromEncryptedData(string data)
    {
        var decryptedData = _crypto.Decrypt(data);

        var splitByAmpersand = decryptedData.Split('&');

        if (splitByAmpersand.Length < 2)
        {
            throw new ArgumentException("Invalid encrypted data");
        }

        var token = splitByAmpersand.First(i => i.StartsWith("token=")).Replace("token=", "");

        return Guid.Parse(token);
    }
}
