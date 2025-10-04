using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RealtorApp.Api.Hubs;
using RealtorApp.Api.Middleware;
using RealtorApp.Domain.Interfaces;
using RealtorAppDbContext = RealtorApp.Domain.Models.RealtorAppDbContext;
using RealtorApp.Domain.Services;
using RealtorApp.Domain.Settings;
using System.Threading.RateLimiting;
using FluentValidation.AspNetCore;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.AddSingleton(serviceCollection => serviceCollection.GetRequiredService<IOptions<AppSettings>>().Value);
builder.Services.AddScoped<IUserAuthService, UserAuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthProviderService, FirebaseAuthProviderService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICryptoService, CryptoService>();
builder.Services.AddScoped<ITaskService, TaskService>();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();


// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    // Anonymous endpoints - more restrictive
    options.AddFixedWindowLimiter("Anonymous", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 10;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });

    // Authenticated endpoints - more permissive
    options.AddFixedWindowLimiter("Authenticated", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 100;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });

    // Global rejection response
    options.RejectionStatusCode = 429;
});

builder.Services.AddDbContext<RealtorAppDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.WebHost.UseKestrel(o => o.AddServerHeader = false);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var appSettings = builder.Configuration.Get<AppSettings>() ?? throw new InvalidOperationException("AppSettings not configured");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Signature validation
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(appSettings.Jwt.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = appSettings.Jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = appSettings.Jwt.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            RequireExpirationTime = true,
            NameClaimType = "sub",
            RoleClaimType = "role"
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/websockets/chat"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR().AddJsonProtocol(o =>
    {
        o.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserValidationMiddleware>();

app.MapControllers();
app.MapHub<ChatHub>("/api/websockets/chat");

app.Run();
