using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.HttpOverrides;
using RePlay.Server.Models;
using RePlay.Server.Services;

namespace RePlay.Server;

public static class RequestSecurity
{
    public static void AddRequestSecurity(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (var address in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
                options.KnownProxies.Add(IPAddress.Parse(address));
            foreach (var network in builder.Configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? [])
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        });

        if (!builder.Environment.IsDevelopment() &&
            builder.Configuration["AllowedHosts"] is null or "*" &&
            Uri.TryCreate(builder.Configuration["Spotify:RedirectUri"], UriKind.Absolute, out var callback))
        {
            builder.Services.Configure<HostFilteringOptions>(options => options.AllowedHosts = [callback.Host]);
        }

        builder.Services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(context => !IsLimitedApiRequest(context)
                    ? RateLimitPartition.GetNoLimiter("static")
                    : RateLimitPartition.GetFixedWindowLimiter(PartitionKey(context), _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
                    })),
                PartitionedRateLimiter.Create<HttpContext, string>(context => !IsLimitedApiRequest(context)
                    ? RateLimitPartition.GetNoLimiter("static")
                    : RateLimitPartition.GetConcurrencyLimiter(PartitionKey(context), _ => new ConcurrencyLimiterOptions
                    {
                        PermitLimit = 4, QueueLimit = 0
                    })));
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await context.HttpContext.Response.WriteAsJsonAsync(new ApiError
                {
                    Code = "RATE_LIMITED", Message = "Too many requests. Please try again later."
                }, cancellationToken);
            };
        });
    }

    private static bool IsLimitedApiRequest(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/api") &&
        !(HttpMethods.IsPost(context.Request.Method) && context.Request.Path.Equals("/api/auth/logout", StringComparison.OrdinalIgnoreCase));

    public static void UseRequestSecurity(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseExceptionHandler(handler => handler.Run(async context =>
        {
            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            var status = exception is BadHttpRequestException badRequest ? badRequest.StatusCode : 500;
            context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RequestErrors")
                .LogError(exception, "Request failed with status {StatusCode}", status);
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new ApiError
            {
                Code = status == 400 ? "INVALID_REQUEST" : "INTERNAL_SERVER_ERROR",
                Message = status == 400 ? "The request could not be read. Check the request body and parameters." : "An unexpected error occurred."
            });
        }));
        app.UseStatusCodePages(async status =>
        {
            if (!status.HttpContext.Request.Path.StartsWithSegments("/api")) return;
            await status.HttpContext.Response.WriteAsJsonAsync(new ApiError
            {
                Code = status.HttpContext.Response.StatusCode == 400 ? "INVALID_REQUEST" : "HTTP_ERROR",
                Message = "The request could not be completed. Check the request body and parameters."
            });
        });
        app.UseRateLimiter();
    }

    private static string PartitionKey(HttpContext context)
    {
        var id = context.Request.Cookies["replay_session_id"];
        var session = id is null ? null : context.RequestServices.GetRequiredService<ISessionStore>().GetSession(id);
        return session is null ? $"ip:{context.Connection.RemoteIpAddress}" : $"user:{session.User.Id}";
    }
}
