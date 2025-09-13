using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RealtorApp.Domain.Models;
using RealtorApp.Domain.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();

builder.Services.AddSingleton(serviceCollection => serviceCollection.GetRequiredService<IOptions<AppSettings>>().Value);

builder.Services.AddDbContext<RealtorAppDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.WebHost.UseKestrel(o => o.AddServerHeader = false);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
