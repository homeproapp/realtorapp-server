namespace RealtorApp.Domain.Settings;

public class AppSettings()
{
    public byte UserIdCacheExpirationInMins { get; set; }
    public byte ConversationParticipantsCacheExpirationInMins { get; set; }
    public byte UsersAssignedToPropertyCacheExpirationInMins { get; set; }
    public string FrontendBaseUrl { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = "HomePro";
    public JwtSettings Jwt { get; set; } = new();
    public FirebaseSettings Firebase { get; set; } = new();
    public EmailSettings Email { get; set; } = new();
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

public class EmailSettings
{
    public SmtpSettings Smtp { get; set; } = new();
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}