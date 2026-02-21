using RePlay.Server.Configuration;
using RePlay.Server.Endpoints;
using RePlay.Server.Services;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text.Json.Serialization;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    // ACA terminates TLS and forwards headers through infrastructure proxies.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Configure Spotify options with validation
builder.Services.AddOptions<SpotifyOptions>()
    .Bind(builder.Configuration.GetSection(SpotifyOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options =>
    {
        if (string.IsNullOrWhiteSpace(options.ClientId))
            return false;
        if (string.IsNullOrWhiteSpace(options.ClientSecret))
            return false;
        if (string.IsNullOrWhiteSpace(options.RedirectUri))
            return false;
        if (options.RedirectUri.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }, "Spotify configuration is invalid. Ensure ClientId, ClientSecret, and RedirectUri are set. RedirectUri must not contain 'localhost'.")
    .ValidateOnStart();

// Configure external source options
builder.Services.Configure<LastfmOptions>(builder.Configuration.GetSection(LastfmOptions.SectionName));
builder.Services.AddOptions<DiscogsOptions>()
    .Bind(builder.Configuration.GetSection(DiscogsOptions.SectionName))
    .ValidateDataAnnotations();
builder.Services.Configure<SetlistFmOptions>(builder.Configuration.GetSection(SetlistFmOptions.SectionName));

// Register HTTP client for Spotify API
builder.Services.AddHttpClient<ISpotifyAuthService, SpotifyAuthService>();

// Register HTTP client for Last.fm API
builder.Services.AddHttpClient<ILastfmService, LastfmService>();

// Register HTTP client for Discogs API
builder.Services.AddHttpClient<IDiscogsService, DiscogsService>();

// Register HTTP client for Setlist.fm API
builder.Services.AddHttpClient<ISetlistFmService, SetlistFmService>();

// Register HTTP client for Spotify matching service
builder.Services.AddHttpClient<ISpotifyMatchingService, SpotifyMatchingService>();

// Register session storage (in-memory for now)
builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>();

// Use string-based enums for request/response JSON
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// API routes
var api = app.MapGroup("/api");

// Auth endpoints
var auth = api.MapGroup("/auth");
auth.MapAuthEndpoints();

// Configuration endpoints
api.MapConfigurationEndpoints();

// Matching endpoints
api.MapMatchingEndpoints();

// Playlist endpoints
api.MapPlaylistEndpoints();

// Data sources endpoints
api.MapSourcesEndpoints();

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();
