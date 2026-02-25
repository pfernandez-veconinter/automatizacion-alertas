using Quartz;
using TeamsNotificationService.Services;

namespace TeamsNotificationService.Jobs;

[DisallowConcurrentExecution]
public class PaymentLogMonitorJob(
    IPaymentLogMonitorService monitorService,
    ITeamsWebhookService webhookService,
    ILogger<PaymentLogMonitorJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Executing payment log monitor job at {Now}", DateTimeOffset.Now);

        var summary = await monitorService.GetSummaryAsync(context.CancellationToken);

        var payload = AdaptiveCardFactory.CreatePaymentLogSummaryCard(summary);

        await webhookService.SendAdaptiveCardAsync(payload, context.CancellationToken);
    }
}
