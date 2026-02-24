using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TeamsNotificationService.Models;

namespace TeamsNotificationService.Services;

public interface ITeamsWebhookService
{
    Task SendAdaptiveCardAsync(AdaptiveCardPayload payload, CancellationToken cancellationToken = default);
}

public class TeamsWebhookService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<TeamsWebhookService> logger) : ITeamsWebhookService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task SendAdaptiveCardAsync(AdaptiveCardPayload payload, CancellationToken cancellationToken = default)
    {
        var webhookUrl = configuration["Teams:WebhookUrl"];

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            logger.LogWarning("Teams webhook URL is not configured. Skipping notification.");
            return;
        }

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        logger.LogDebug("Sending payload: {Json}", json);

        using var client = httpClientFactory.CreateClient("TeamsWebhook");
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(webhookUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();
            logger.LogInformation("Teams notification sent successfully.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to send Teams notification.");
            throw;
        }
    }
}
