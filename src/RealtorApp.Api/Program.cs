using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RealtorApp.Api.Hubs;
using RealtorApp.Api.Middleware;
using RealtorApp.Domain.Interfaces;
using RealtorAppDbContext = RealtorApp.Domain.Models.RealtorAppDbContext;
using RealtorApp.Domain.Services;
using RealtorApp.Domain.Settings;

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

            // Issuer validation
            ValidateIssuer = true,
            ValidIssuer = appSettings.Jwt.Issuer,

            // Audience validation
            ValidateAudience = true,
            ValidAudience = appSettings.Jwt.Audience,

            // Expiry validation
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),

            // Require expiry
            RequireExpirationTime = true,

            // Custom claims validation
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
builder.Services.AddSignalR()
    .AddJsonProtocol(o =>
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

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserValidationMiddleware>();

app.MapControllers();
app.MapHub<ChatHub>("/api/websockets/chat");

app.Run();
