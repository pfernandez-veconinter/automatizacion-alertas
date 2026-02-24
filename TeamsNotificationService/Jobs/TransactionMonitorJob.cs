using Quartz;
using TeamsNotificationService.Services;

namespace TeamsNotificationService.Jobs;

[DisallowConcurrentExecution]
public class TransactionMonitorJob(
    ITransactionMonitorService monitorService,
    ITeamsWebhookService webhookService,
    ILogger<TransactionMonitorJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Executing transaction monitor job at {Now}", DateTimeOffset.Now);

        var summary = await monitorService.GetSummaryAsync(context.CancellationToken);

        var payload = AdaptiveCardFactory.CreateTransactionSummaryCard(summary);

        await webhookService.SendAdaptiveCardAsync(payload, context.CancellationToken);
    }
}
