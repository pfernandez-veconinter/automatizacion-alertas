using Microsoft.Data.SqlClient;
using TeamsNotificationService.Models;

namespace TeamsNotificationService.Services;

public interface ITransactionMonitorService
{
    Task<TransactionSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public class TransactionMonitorService(
    IConfiguration configuration,
    ILogger<TransactionMonitorService> logger) : ITransactionMonitorService
{
    private static readonly HashSet<string> AllowedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "TRX_Online_Card", "TRX_Online_Bank", "TRX_Online_Bank_PM",
        "TRX_Online_BHD", "TRX_Online_PayPal", "TRX_Online_PIX",
        "TRX_Online_Stripe", "TRX_Release_Now"
    };

    private static readonly HashSet<string> AllowedColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "date_trx", "registration_date", "origin_bank", "origin_payment_country", "country"
    };

    // Initialized on first GetSummaryAsync call to capture the moment monitoring begins.
    private DateTime? _lastCheckedTime;
    private long? _lastReleaseNowMaxId;
    private readonly object _stateLock = new();

    public async Task<TransactionSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("VeconinterWeb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("Connection string 'VeconinterWeb' is not configured. Skipping transaction query.");
            var now = DateTime.Now;
            return new TransactionSummary { FromTime = now, ToTime = now };
        }

        DateTime from;
        DateTime to = DateTime.Now;
        lock (_stateLock)
        {
            // On the very first execution, treat the start of this call as the baseline.
            from = _lastCheckedTime ?? to;
        }

        var summary = new TransactionSummary { FromTime = from, ToTime = to };

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        summary.Tables.Add(await QueryGroupedByDateAsync(connection,
            "TRX_Online_Card", "date_trx", "origin_payment_country", from, to, cancellationToken));

        summary.Tables.Add(await QueryGroupedByDateAsync(connection,
            "TRX_Online_Bank", "date_trx", "origin_bank", from, to, cancellationToken));

        summary.Tables.Add(await QueryGroupedByDateAsync(connection,
            "TRX_Online_Bank_PM", "date_trx", "origin_bank", from, to, cancellationToken));

        summary.Tables.Add(await QueryGroupedByDateAsync(connection,
            "TRX_Online_BHD", "date_trx", "country", from, to, cancellationToken));

        summary.Tables.Add(await QueryGroupedByDateAsync(connection,
            "TRX_Online_PayPal", "date_trx", "origin_payment_country", from, to, cancellationToken));

        summary.Tables.Add(await QueryGroupedByDateAsync(connection,
            "TRX_Online_PIX", "date_trx", "country", from, to, cancellationToken));

        summary.Tables.Add(await QueryGroupedByDateAsync(connection,
            "TRX_Online_Stripe", "date_trx", "country", from, to, cancellationToken));

        summary.Tables.Add(await QueryReleaseNowAsync(connection, cancellationToken));

        lock (_stateLock)
        {
            _lastCheckedTime = to;
        }

        return summary;
    }

    private async Task<TableSummary> QueryGroupedByDateAsync(
        SqlConnection connection,
        string tableName,
        string dateColumn,
        string groupColumn,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        var table = new TableSummary { TableName = tableName };

        if (!AllowedTables.Contains(tableName) || !AllowedColumns.Contains(dateColumn) || !AllowedColumns.Contains(groupColumn))
        {
            logger.LogError("Rejected query with unexpected table or column name: {Table}, {DateCol}, {GroupCol}", tableName, dateColumn, groupColumn);
            return table;
        }

        try
        {
            var sql = $"""
                SELECT ISNULL([{groupColumn}], 'N/A') AS GroupKey, COUNT(*) AS Cnt
                FROM [dbo].[{tableName}]
                WHERE [{dateColumn}] >= @from AND [{dateColumn}] < @to
                GROUP BY [{groupColumn}]
                ORDER BY Cnt DESC
                """;

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@from", from);
            cmd.Parameters.AddWithValue("@to", to);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                table.Groups.Add(new GroupCount
                {
                    GroupKey = reader.GetString(0),
                    Count = reader.GetInt32(1)
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error querying table {TableName}", tableName);
        }
        return table;
    }

    private async Task<TableSummary> QueryReleaseNowAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        var table = new TableSummary { TableName = "TRX_Release_Now" };
        try
        {
            // On first call, initialize the baseline max ID and return 0 counts.
            long lastId;
            bool firstRun = false;
            lock (_stateLock)
            {
                if (_lastReleaseNowMaxId is null)
                {
                    firstRun = true;
                    lastId = 0;
                }
                else
                {
                    lastId = _lastReleaseNowMaxId.Value;
                }
            }

            if (firstRun)
            {
                var maxIdSql = "SELECT ISNULL(MAX(id), 0) FROM [dbo].[TRX_Release_Now]";
                await using var maxCmd = new SqlCommand(maxIdSql, connection);
                var maxId = Convert.ToInt64(await maxCmd.ExecuteScalarAsync(cancellationToken));
                lock (_stateLock) { _lastReleaseNowMaxId = maxId; }
                return table; // zero counts on first run
            }

            const string sql = """
                SELECT ISNULL([country], 'N/A') AS GroupKey, COUNT(*) AS Cnt
                FROM [dbo].[TRX_Release_Now]
                WHERE [id] > @lastId
                GROUP BY [country]
                ORDER BY Cnt DESC
                """;

            const string maxNewIdSql = "SELECT ISNULL(MAX(id), 0) FROM [dbo].[TRX_Release_Now] WHERE [id] > @lastId";

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@lastId", lastId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                table.Groups.Add(new GroupCount
                {
                    GroupKey = reader.GetString(0),
                    Count = reader.GetInt32(1)
                });
            }
            await reader.CloseAsync();

            await using var maxNewCmd = new SqlCommand(maxNewIdSql, connection);
            maxNewCmd.Parameters.AddWithValue("@lastId", lastId);
            var newMaxId = Convert.ToInt64(await maxNewCmd.ExecuteScalarAsync(cancellationToken));

            lock (_stateLock) { _lastReleaseNowMaxId = newMaxId > lastId ? newMaxId : lastId; }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error querying table TRX_Release_Now");
        }
        return table;
    }
}
