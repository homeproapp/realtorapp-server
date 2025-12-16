using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Settings;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace RealtorApp.Domain.Services;

public class FirebaseAuthProviderService(
    AppSettings appSettings,
    ILogger<FirebaseAuthProviderService> logger,
    IHttpClientFactory httpClientFactory) : IAuthProviderService
{
    private readonly AppSettings _appSettings = appSettings;
    private readonly ILogger<FirebaseAuthProviderService> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private static FirebaseApp? _firebaseApp;
    private static readonly Lock _lock = new();

    private FirebaseApp GetFirebaseApp()
    {
        if (_firebaseApp != null)
            return _firebaseApp;

        lock (_lock)
        {
            if (_firebaseApp != null)
                return _firebaseApp;

            _firebaseApp = FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.GetApplicationDefault(),
                ProjectId = _appSettings.Firebase.ProjectId
            });

            return _firebaseApp;
        }
    }

    public async Task<UserRecord?> RegisterWithEmailAndPasswordAsync(string email, string password, bool emailVerified)
    {
        try
        {
            var auth = FirebaseAuth.GetAuth(GetFirebaseApp());
            var user = new UserRecordArgs()
            {
                Email = email,
                EmailVerified = emailVerified,
                Password = password
            };

            var result = await auth.CreateUserAsync(user);

            return result;
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogError(ex, "Firebase error creating user: {ErrorCode}", ex.AuthErrorCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Message}", ex.Message);
            return null;
        }
    }

    public async Task<AuthProviderUserDto?> SignInWithEmailAndPasswordAsync(string email, string password)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var requestBody = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var apiKey = _appSettings.Firebase.ApiKey;
            var response = await httpClient.PostAsJsonAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}",
                requestBody
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Firebase sign-in failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<FirebaseSignInResponse>();
            if (result?.IdToken == null)
            {
                return null;
            }

            return await ValidateTokenAsync(result.IdToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing in: {Message}", ex.Message);
            return null;
        }
    }

    public async Task<AuthProviderUserDto?> ValidateTokenAsync(string providerToken)
    {
        try
        {
            // Verify the Firebase ID token using Firebase Admin SDK
            var firebaseApp = GetFirebaseApp();
            var decodedToken = await FirebaseAuth.GetAuth(firebaseApp).VerifyIdTokenAsync(providerToken);

            // Extract user information from the decoded token
            return new AuthProviderUserDto
            {
                Uid = decodedToken.Uid,
                Email = decodedToken.Claims.TryGetValue("email", out var email) ? email.ToString() ?? string.Empty : string.Empty,
                DisplayName = decodedToken.Claims.TryGetValue("name", out var name) ? name.ToString() : null
            };
        }
        catch (FirebaseAuthException ex)
        {
            // Token is invalid, expired, or malformed
            _logger.LogError(ex, "FirebaseAuthException - {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception - {Message}", ex.Message);
            return null;
        }
    }
}

internal class FirebaseSignInResponse
{
    [JsonPropertyName("idToken")]
    public string? IdToken { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expiresIn")]
    public string? ExpiresIn { get; set; }

    [JsonPropertyName("localId")]
    public string? LocalId { get; set; }
}
