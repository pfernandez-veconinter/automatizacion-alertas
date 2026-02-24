using Quartz;
using TeamsNotificationService.Jobs;
using TeamsNotificationService.Services;

var builder = Host.CreateApplicationBuilder(args);

// Register HttpClient for Teams webhook
builder.Services.AddHttpClient("TeamsWebhook");

// Register Teams webhook service
builder.Services.AddSingleton<ITeamsWebhookService, TeamsWebhookService>();

// Resolve configured timezone before Quartz setup so we can log fallback
var configuredTimezone = builder.Configuration["Schedule:TimeZone"] ?? "America/Caracas";
TimeZoneInfo scheduleTimezone;
bool timezoneFallback = false;
try
{
    scheduleTimezone = TimeZoneInfo.FindSystemTimeZoneById(configuredTimezone);
}
catch (TimeZoneNotFoundException)
{
    scheduleTimezone = TimeZoneInfo.Local;
    timezoneFallback = true;
}

// Configure Quartz scheduler
builder.Services.AddQuartz(q =>
{
    // Define the four daily notification jobs
    var schedules = new[]
    {
        ("Morning",   "0 0 8  * * ?"),   // 8:00 AM
        ("Midday",    "0 0 12 * * ?"),   // 12:00 PM
        ("Afternoon", "0 0 15 * * ?"),   // 3:00 PM
        ("Evening",   "0 0 17 * * ?")    // 5:00 PM
    };

    var timeLabels = new Dictionary<string, string>
    {
        ["Morning"]   = "Buenos días - 8:00 AM",
        ["Midday"]    = "Mediodía - 12:00 PM",
        ["Afternoon"] = "Buenas tardes - 3:00 PM",
        ["Evening"]   = "Final del día - 5:00 PM"
    };

    foreach (var (name, cron) in schedules)
    {
        var jobKey = new JobKey($"TeamsNotification-{name}", "TeamsNotifications");

        q.AddJob<TeamsNotificationJob>(opts => opts
            .WithIdentity(jobKey)
            .UsingJobData("TimeLabel", timeLabels[name]));

        q.AddTrigger(opts => opts
            .ForJob(jobKey)
            .WithIdentity($"Trigger-{name}", "TeamsNotifications")
            .WithCronSchedule(cron, x => x.InTimeZone(scheduleTimezone)));
    }
});

builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
});

var host = builder.Build();

if (timezoneFallback)
{
    var startupLogger = host.Services.GetRequiredService<ILogger<Program>>();
    startupLogger.LogWarning(
        "Timezone '{ConfiguredTimezone}' not found. Falling back to local timezone: {LocalTimezone}",
        configuredTimezone, TimeZoneInfo.Local.Id);
}

host.Run();

