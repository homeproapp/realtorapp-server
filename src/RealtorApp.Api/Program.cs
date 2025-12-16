using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RealtorApp.Api.Hubs;
using RealtorApp.Api.Middleware;
using RealtorApp.Domain.Interfaces;
using RealtorAppDbContext = RealtorApp.Infra.Data.RealtorAppDbContext;
using RealtorApp.Domain.Services;
using RealtorApp.Domain.Settings;
using System.Threading.RateLimiting;
using FluentValidation.AspNetCore;
using FluentValidation;
using RealtorApp.Domain.Constants;
using RealtorApp.Api.Policies;
using Microsoft.AspNetCore.Authorization;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
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
builder.Services.AddScoped<IListingService, ListingService>();
builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services.AddScoped<IImagesService, ImagesService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddScoped<IContactsService, ContactsService>();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddSingleton<IAuthorizationHandler, OneOfRolesHandler>();

var allowAll = "allowAll";

builder.Services.AddCors(options =>
{
    options.AddPolicy(allowAll, policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

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

        options.MapInboundClaims = false;

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


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyConstants.AgentOnly, policy =>
        policy.RequireRole(RoleConstants.Agent));

    options.AddPolicy(PolicyConstants.ClientOnly, policy =>
        policy.RequireRole(RoleConstants.Client));

    options.AddPolicy(PolicyConstants.ClientOrAgent, policy =>
        policy.AddRequirements(new OneOfRolesRequirement([RoleConstants.Agent, RoleConstants.Client])));
});

builder.Services.AddSignalR().AddJsonProtocol(o =>
    {
        o.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
var isDev = app.Environment.IsDevelopment();
if (isDev)
{
    app.MapOpenApi();
}

app.UseCors(allowAll);

if (!isDev)
{
    app.UseHttpsRedirection();
}


app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserValidationMiddleware>();

app.MapControllers();
app.MapHub<ChatHub>("/api/websockets/chat");

app.Run();
