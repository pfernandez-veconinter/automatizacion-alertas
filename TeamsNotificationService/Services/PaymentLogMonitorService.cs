using Microsoft.Data.SqlClient;
using TeamsNotificationService.Models;

namespace TeamsNotificationService.Services;

public interface IPaymentLogMonitorService
{
    Task<PaymentLogSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public class PaymentLogMonitorService(
    IConfiguration configuration,
    ILogger<PaymentLogMonitorService> logger) : IPaymentLogMonitorService
{
    // Initialized on first GetSummaryAsync call to capture the moment monitoring begins.
    private DateTime? _lastCheckedTime;
    private readonly object _stateLock = new();

    public async Task<PaymentLogSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("VeconinterWeb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("Connection string 'VeconinterWeb' is not configured. Skipping payment log query.");
            var now = DateTime.Now;
            return new PaymentLogSummary { FromTime = now, ToTime = now };
        }

        DateTime from;
        DateTime to = DateTime.Now;
        lock (_stateLock)
        {
            from = _lastCheckedTime ?? to;
        }

        var summary = new PaymentLogSummary { FromTime = from, ToTime = to };

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        try
        {
            const string sql = """
                SELECT
                    ISNULL([payment_method], 'N/A') AS PaymentMethod,
                    ISNULL([country_id], 'N/A')     AS CountryId,
                    CASE
                        WHEN [collection_id_real] IS NOT NULL AND [collection_id_real] <> '' THEN 1
                        ELSE 0
                    END AS IsProcessed,
                    COUNT(*) AS Cnt
                FROM [dbo].[Payment_Log]
                WHERE [date] >= @from AND [date] < @to
                GROUP BY [payment_method], [country_id],
                    CASE
                        WHEN [collection_id_real] IS NOT NULL AND [collection_id_real] <> '' THEN 1
                        ELSE 0
                    END
                ORDER BY IsProcessed DESC, Cnt DESC
                """;

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@from", from);
            cmd.Parameters.AddWithValue("@to", to);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var entry = new PaymentGroupCount
                {
                    PaymentMethod = reader.GetString(0),
                    CountryId = reader.GetString(1),
                    Count = reader.GetInt32(3)
                };
                var isProcessed = reader.GetInt32(2) == 1;
                if (isProcessed)
                    summary.Processed.Add(entry);
                else
                    summary.NotProcessed.Add(entry);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error querying Payment_Log table");
        }

        lock (_stateLock)
        {
            _lastCheckedTime = to;
        }

        return summary;
    }
}
