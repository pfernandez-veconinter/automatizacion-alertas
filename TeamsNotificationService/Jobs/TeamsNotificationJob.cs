using Quartz;
using TeamsNotificationService.Services;

namespace TeamsNotificationService.Jobs;

[DisallowConcurrentExecution]
public class TeamsNotificationJob(
    ITeamsWebhookService webhookService,
    ILogger<TeamsNotificationJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var timeLabel = context.JobDetail.JobDataMap.GetString("TimeLabel") ?? "Notificación";

        logger.LogInformation("Executing Teams notification job: {TimeLabel} at {Now}", timeLabel, DateTimeOffset.Now);

        var payload = AdaptiveCardFactory.CreateScheduledNotification(timeLabel, DateTime.Now);

        await webhookService.SendAdaptiveCardAsync(payload, context.CancellationToken);
    }
}
