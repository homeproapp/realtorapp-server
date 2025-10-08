namespace RealtorApp.Domain.Settings;

public class AppSettings()
{
    public byte UserIdCacheExpirationInMins { get; set; }
    public byte ConversationParticipantsCacheExpirationInMins { get; set; }
    public byte UsersAssignedToPropertyCacheExpirationInMins { get; set; }
    public int ImageCacheDurationInSeconds { get; set; } = 86400;
    public string FrontendBaseUrl { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = "HomePro";
    public JwtSettings Jwt { get; set; } = new();
    public FirebaseSettings Firebase { get; set; } = new();
    public required AwsSettings Aws { get; set; }
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

public class AwsSettings
{
    public required string AccessKey { get; set; }
    public required string SecretKey { get; set; }
    public required S3Settings S3 { get; set; }
    public required SesSettings Ses { get; set; }
}

public class S3Settings
{
    public required string BucketNamePrefix { get; set; }
    public required string Region { get; set; }
    public required string ImagesBucketName { get; set; }
}

public class SesSettings
{
    public required string Region { get; set; }
    public required string FromEmail { get; set; }
    public required string FromName { get; set; }
}