using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VERA.Server.Data;
using VERA.Server.Services;

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

builder.Services.AddHsts(opt =>
{
    opt.MaxAge            = TimeSpan.FromDays(365);
    opt.IncludeSubDomains = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VeraDbContext>();
    db.Database.Migrate();
}

app.UseHsts();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

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
