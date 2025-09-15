using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RealtorApp.Api.Hubs;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Models;
using RealtorApp.Domain.Services;
using RealtorApp.Domain.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();

builder.Services.AddSingleton(serviceCollection => serviceCollection.GetRequiredService<IOptions<AppSettings>>().Value);
builder.Services.AddScoped<IUserAuthService, UserAuthService>();

builder.Services.AddDbContext<RealtorAppDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.WebHost.UseKestrel(o => o.AddServerHeader = false);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/websockets/chat"))
                    context.Token = accessToken;
                return System.Threading.Tasks.Task.CompletedTask;
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
app.MapControllers();
app.MapHub<ChatHub>("/api/websockets/chat");

app.UseAuthentication();
app.UseAuthorization();

app.Run();
