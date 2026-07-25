using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Grained.Api.Auth;
using Grained.Api.Endpoints;
using Grained.Application;
using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Domain.Entities;
using Grained.Infrastructure;
using Grained.Infrastructure.Persistence;
using Grained.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- Options ---
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

// --- Application + Infrastructure (reused unchanged) ---
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Identity core (no cookie schemes — the API authenticates with JWT bearer only)
builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// --- Current user + token issuing ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
builder.Services.AddSingleton<JwtTokenService>();

// --- Auth ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", p => p.RequireRole(Grained.Domain.Common.Roles.SuperAdmin));
    options.AddPolicy("ChurchAdmin", p => p.RequireRole(Grained.Domain.Common.Roles.ChurchAdmin));
    // "Staff" = ChurchAdmin or Teacher (read access to children/lessons, take attendance, view reports).
    options.AddPolicy("Staff", p => p.RequireRole(Grained.Domain.Common.Roles.ChurchAdmin, Grained.Domain.Common.Roles.Teacher));
    options.AddPolicy("Parent", p => p.RequireRole(Grained.Domain.Common.Roles.Parent));
});

// --- CORS ---
// Allowlist only, from `Cors:AllowedOrigins`. Credentials are deliberately NOT allowed: auth is a
// bearer token the app sends explicitly, never an ambient cookie, so an allowlisted origin still
// can't ride along on someone's session. In Development we fall back to the Vite dev/preview
// servers; in any other environment an unconfigured list means *no* cross-origin calls (fail closed
// — the web app is expected to be same-origin behind a reverse proxy).
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (corsOrigins.Length == 0 && builder.Environment.IsDevelopment())
{
    corsOrigins = ["http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:4173"];
}
builder.Services.AddCors(o => o.AddPolicy("web", p =>
{
    if (corsOrigins.Length == 0) return; // no origins configured => policy emits no CORS headers
    p.WithOrigins(corsOrigins)
     .AllowAnyHeader()
     .AllowAnyMethod()
     .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
}));

// Persist DataProtection keys so invite tokens survive app restarts.
builder.Services.AddDataProtection();

// Rate-limit the public invite endpoints (token guessing / abuse).
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddFixedWindowLimiter("invite", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 20;
        opt.QueueLimit = 0;
    });
    o.AddFixedWindowLimiter("public", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 60;
        opt.QueueLimit = 0;
    });
});

var app = builder.Build();

// Apply migrations + seed roles / SuperAdmin on startup, plus the demo "Grace Community" sample data
// only when `SeedSampleData` is true (opt-in, so a wiped DB stays clean across restarts). Roles and
// the SuperAdmin account are always ensured. (DbSeeder is idempotent.)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var seedSampleData = builder.Configuration.GetValue("SeedSampleData", false);
    await DbSeeder.SeedAsync(db, roleManager, userManager, seedSampleData);
}

// Translate application validation errors into 400s.
app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (ValidationException ex)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsJsonAsync(new { message = ex.Message });
    }
});

app.UseCors("web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "Grained API", status = "ok" }));
app.MapAuthEndpoints();
app.MapDashboardEndpoints();
app.MapInviteEndpoints();
app.MapChurchEndpoints();
app.MapClassGroupEndpoints();
app.MapChildrenEndpoints();
app.MapTeacherEndpoints();
app.MapTeacherWorkspaceEndpoints();
app.MapParentEndpoints();
app.MapLessonEndpoints();
app.MapEventEndpoints();
app.MapCampaignEndpoints();
app.MapPublicEndpoints();
app.MapBadgeEndpoints();
app.MapAnnouncementEndpoints();
app.MapGrowthEndpoints();
app.MapAttendanceEndpoints();
app.MapReportEndpoints();

app.Run();
