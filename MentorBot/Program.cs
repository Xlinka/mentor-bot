using MentorBot;
using MentorBot.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;

// Create a builder for the WebApplication
var builder = WebApplication.CreateBuilder(args);

// Get the mentor configuration section
var mentorConfig = builder.Configuration.GetSection("mentors");

// Add mentor options to the service collection
builder.Services.Configure<MentorOptions>(mentorConfig);

// Check if Swagger is enabled in the configuration
var hasSwagger = mentorConfig.Get<MentorOptions>()?.EnableSwagger ?? false;

// Add the ticket notifier singleton to the service collection
builder.Services.AddSingleton<ITicketNotifier, TicketNotifier>();

// Add the discord context to the service collection
builder.Services.AddDiscordContext(builder.Configuration);

// Add the Neos HTTP client to the service collection
builder.Services.AddNeosHttpClient(builder.Configuration);

// Add the signal context to the service collection
builder.Services.AddSignalContexts(builder.Configuration);

// Add the token generator as a transient service
builder.Services.AddTransient<ITokenGenerator, TokenGenerator>();

// Add Swagger if it is enabled
if (hasSwagger)
{
    builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mentor Signal", Version = "v1" }));
}

// Add health checks and signal health checks
builder.Services.AddHealthChecks()
  .AddSignalHealthChecks();

// Configure JSON options for Mentor
builder.Services.Configure<JsonOptions>(options =>
  options.SerializerOptions.ConfigureForMentor());

// Add JSON options for Mentor to the controllers
builder.Services.AddControllers().AddJsonOptions(opts =>
  opts.JsonSerializerOptions.ConfigureForMentor());

// Add cookie authentication
builder.Services.AddAuthentication(c => c.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme)
  .AddCookie(c => c.ExpireTimeSpan = TimeSpan.FromHours(3));

// Add Razor Pages
builder.Services.AddRazorPages();

// Build the web application
var app = builder.Build();

// Ensure the database is created
app.EnsureDatabaseCreated();

// If the environment is not development, use exception handling, HSTS, and HTTPS redirection
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    // Otherwise, use the developer exception page
    app.UseDeveloperExceptionPage();
}

// Use authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

if (hasSwagger)
{
  app.UseSwagger();
  app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mentor Signal v1"));
}

app.UseWebSockets(new WebSocketOptions
{
  KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.MapHealthChecks("/health");
app.MapControllers();
app.MapRazorPages();

if (hasSwagger)
{
  app.MapSwagger();
}

app.Run();

// This is needed so integration tests succeed.
public partial class Program { }
