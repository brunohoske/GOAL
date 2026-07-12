using FirebaseAdmin;
using Goal.Application.Abstractions;
using Goal.Application.Completions;
using Goal.Infrastructure.Jobs;
using Goal.Infrastructure.Persistence;
using Goal.Infrastructure.Push;
using Goal.Infrastructure.Services;
using Goal.Infrastructure.Storage;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Goal.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres");

        services.AddSingleton<AuditInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseNpgsql(connectionString)
                   .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Core services
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.Configure<JwtSettings>(config.GetSection(JwtSettings.SectionName));
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IPushSender, FcmPushSender>();
        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.AddScoped<IJobScheduler, HangfireJobScheduler>();
        services.AddScoped<CompletionResolver>();

        // Jobs (resolved by Hangfire's activator)
        services.AddScoped<CompletionDeadlineJob>();
        services.AddScoped<SprintCloserJob>();
        services.AddScoped<NotificationEscalationJob>();

        // Hangfire on PostgreSQL
        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer();

        InitializeFirebase(config);

        return services;
    }

    private static void InitializeFirebase(IConfiguration config)
    {
        var credentialsPath = config["Firebase:CredentialsPath"];
        if (FirebaseApp.DefaultInstance is null && !string.IsNullOrWhiteSpace(credentialsPath) && File.Exists(credentialsPath))
        {
            FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromFile(credentialsPath) });
        }
    }

    /// <summary>Registers Hangfire recurring jobs. Call after the app is built.</summary>
    public static void RegisterRecurringJobs(IRecurringJobManager jobs)
    {
        jobs.AddOrUpdate<SprintCloserJob>("sprint-closer", j => j.RunAsync(), "*/15 * * * *");
        jobs.AddOrUpdate<NotificationEscalationJob>("notification-escalation", j => j.RunAsync(), "*/10 * * * *");
    }
}
