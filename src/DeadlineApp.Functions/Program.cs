using DeadlineApp.Core.Interfaces;
using DeadlineApp.Core.Services;
using DeadlineApp.Infrastructure.Data;
using DeadlineApp.Infrastructure.Data.Repositories;
using DeadlineApp.Infrastructure.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;
using Azure.Identity;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Add DbContext
        services.AddDbContext<DeadlineDbContext>(options =>
            options.UseSqlServer(
                context.Configuration["ConnectionStrings:DefaultConnection"],
                sqlOptions => sqlOptions.EnableRetryOnFailure()));

        // Add Microsoft Graph client
        services.AddScoped<GraphServiceClient>(sp =>
        {
            var credential = new ClientSecretCredential(
                context.Configuration["AzureAd:TenantId"],
                context.Configuration["AzureAd:ClientId"],
                context.Configuration["AzureAd:ClientSecret"]);

            return new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
        });

        // Register repositories
        services.AddScoped<IMatterRepository, MatterRepository>();
        services.AddScoped<IDeadlineRepository, DeadlineRepository>();
        services.AddScoped<ICourtRuleRepository, CourtRuleRepository>();

        // Register services
        services.AddScoped<IDeadlineCalculator, DeadlineCalculator>();
        services.AddScoped<IHolidayService, HolidayService>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<ICalendarSyncService, CalendarSyncService>();
        services.AddScoped<ICurrentUserService, FunctionCurrentUserService>();
    })
    .Build();

host.Run();

// Minimal implementation for Functions context
public class FunctionCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? FirmId => null;
    public string? Email => "system@function";
    public string? EntraObjectId => null;
    public string? IpAddress => null;
    public string? UserAgent => "Azure Functions";
    public bool IsAuthenticated => true;
    public IEnumerable<string> Roles => new[] { "System" };
    public bool IsInRole(string role) => role == "System";
}
