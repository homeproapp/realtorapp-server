using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Settings;

namespace RealtorApp.Domain.Services;

public class FirebaseAuthProviderService(AppSettings appSettings) : IAuthProviderService
{
    private readonly AppSettings _appSettings = appSettings;
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
        catch (FirebaseAuthException)
        {
            // Token is invalid, expired, or malformed
            return null;
        }
        catch (Exception)
        {
            // Other errors (network, configuration, etc.)
            return null;
        }
    }
}