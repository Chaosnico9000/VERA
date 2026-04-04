using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VERA.Server.Data;
using VERA.Server.Services;
using VERA.Shared;
using VERA.Shared.Dto;

var builder = WebApplication.CreateBuilder(args);

var dataDir = builder.Configuration["DataDirectory"] ?? "/home/container/data";
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "vera.db");

builder.Services.AddDbContext<VeraDbContext>(opt =>
    opt.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<AuthService>();
builder.Services.AddControllers();

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret fehlt in der Konfiguration.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew                = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization();

// Pelican/Docker: HTTPS wird am Reverse-Proxy terminiert – ForwardedHeaders weitergeben
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VeraDbContext>();
    db.Database.Migrate();
}

app.UseForwardedHeaders();
app.UseAuthentication();
app.UseAuthorization();

// Veraltete Clients abweisen (426 Upgrade Required)
app.Use(async (ctx, next) =>
{
    if (!ctx.Request.Path.StartsWithSegments("/api/info") &&
        ctx.Request.Headers.TryGetValue("X-Client-Version", out var clientVersionHeader))
    {
        var headerValue = clientVersionHeader.FirstOrDefault();
        if (headerValue != null &&
            Version.TryParse(headerValue,                   out var clientVer) &&
            Version.TryParse(AppVersion.MinClientVersion,   out var minVer)    &&
            clientVer < minVer)
        {
            ctx.Response.StatusCode = 426;
            await ctx.Response.WriteAsJsonAsync(new ApiError(
                "UPDATE_REQUIRED",
                $"Client-Version {headerValue} wird nicht mehr unterstützt. Mindestversion: {AppVersion.MinClientVersion}"));
            return;
        }
    }
    await next();
});

// Einfaches IP-basiertes Rate Limiting für Auth-Endpoints
var rateLimitStore = new ConcurrentDictionary<string, (int Count, DateTime Window)>();
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api/auth"))
    {
        var ip  = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;
        var (count, window) = rateLimitStore.GetOrAdd(ip, _ => (0, now.AddMinutes(1)));
        if (now > window)
            rateLimitStore[ip] = (1, now.AddMinutes(1));
        else if (count >= 20)
        {
            ctx.Response.StatusCode = 429;
            await ctx.Response.WriteAsJsonAsync(new { code = "RATE_LIMITED", message = "Zu viele Anfragen." });
            return;
        }
        else
            rateLimitStore[ip] = (count + 1, window);

    }
    await next();
});

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"]  = "nosniff";
    ctx.Response.Headers["X-Frame-Options"]         = "DENY";
    ctx.Response.Headers["X-XSS-Protection"]        = "1; mode=block";
    ctx.Response.Headers["Referrer-Policy"]         = "no-referrer";
    await next();
});

app.MapControllers();

app.Lifetime.ApplicationStarted.Register(() =>
{
    var logger    = app.Services.GetRequiredService<ILogger<Program>>();
    var endpoints = app.Services.GetRequiredService<IEnumerable<EndpointDataSource>>()
                       .SelectMany(s => s.Endpoints)
                       .OfType<RouteEndpoint>()
                       .OrderBy(e => e.RoutePattern.RawText);

    logger.LogInformation("──────────── VERA Server v{Version} ────────────", AppVersion.Current);
    logger.LogInformation("  Min. Client-Version : {Min}", AppVersion.MinClientVersion);

    var addresses = app.Urls.ToList();
    if (addresses.Count == 0)
    {
        var port = builder.Configuration["ASPNETCORE_HTTP_PORTS"] ?? "8080";
        addresses = [$"http://<server-ip>:{port}"];
    }
    logger.LogInformation("──────────── Server-URL für Clients ────────────");
    foreach (var addr in addresses)
        logger.LogInformation("  → {Url}", addr);
    logger.LogInformation("─────────────────────────────────────────────────");
    logger.LogInformation("──────────── Registrierte Endpoints ────────────");
    foreach (var ep in endpoints)
    {
        var methods = ep.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods
                      ?? ["*"];
        logger.LogInformation("  [{Methods}] /{Route}",
            string.Join(", ", methods),
            ep.RoutePattern.RawText);
    }
    logger.LogInformation("─────────────────────────────────────────────────");
});

app.Run();
