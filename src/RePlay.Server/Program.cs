using RePlay.Server.Configuration;
using RePlay.Server.Endpoints;
using RePlay.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure Spotify options
builder.Services.Configure<SpotifyOptions>(
    builder.Configuration.GetSection(SpotifyOptions.SectionName));

// Configure Last.fm options
builder.Services.Configure<LastfmOptions>(
    builder.Configuration.GetSection(LastfmOptions.SectionName));

// Register HTTP client for Spotify API
builder.Services.AddHttpClient<ISpotifyAuthService, SpotifyAuthService>();

// Register HTTP client for Last.fm API
builder.Services.AddHttpClient<ILastfmService, LastfmService>();

// Register session storage (in-memory for now)
builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// API routes
var api = app.MapGroup("/api");

// Auth endpoints
var auth = api.MapGroup("/auth");
auth.MapAuthEndpoints();

// Configuration endpoints
api.MapConfigurationEndpoints();

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();

