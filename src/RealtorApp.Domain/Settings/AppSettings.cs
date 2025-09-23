namespace RealtorApp.Domain.Settings;

public class AppSettings()
{
    public byte UserIdCacheExpirationInMins { get; set; }
    public byte ConversationParticipantsCacheExpirationInMins { get; set; }
    public byte UsersAssignedToPropertyCacheExpirationInMins { get; set; }
    public JwtSettings Jwt { get; set; } = new();
    public FirebaseSettings Firebase { get; set; } = new();
}

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 30;
}

public class FirebaseSettings
{
    public string ProjectId { get; set; } = string.Empty;
}