using Grained.Application.Auth;
using Grained.Application.Common.Interfaces;
using Grained.Application.Onboarding;
using Grained.Application.Payments;
using Grained.Infrastructure.Auth;
using Grained.Infrastructure.Onboarding;
using Grained.Infrastructure.Payments;
using Grained.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grained.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

        // Transient and constructed directly (not resolved from the DI scope) so every
        // Application service gets its own short-lived DbContext instance. Blazor Server's
        // DI scope lives for the whole circuit (the user's session), so sharing one scoped
        // DbContext across every page visit accumulates stale tracked entities and causes
        // spurious DbUpdateConcurrencyExceptions on later saves. Identity keeps using the
        // scoped ApplicationDbContext registered above via AddEntityFrameworkStores.
        services.AddTransient<IApplicationDbContext>(sp =>
            new ApplicationDbContext(sp.GetRequiredService<DbContextOptions<ApplicationDbContext>>()));

        // Onboarding: invite tokens (DataProtection) + swappable email sender.
        services.AddScoped<IInviteTokenService, InviteTokenService>();
        services.AddScoped<IInviteEmailSender, LoggingInviteEmailSender>();
        services.AddScoped<IPasswordResetEmailSender, LoggingPasswordResetEmailSender>();

        // Payments: structure-first "record payment" gateway; swap for Stripe behind IPaymentGateway.
        services.AddScoped<IPaymentGateway, RecordPaymentGateway>();

        return services;
    }
}
