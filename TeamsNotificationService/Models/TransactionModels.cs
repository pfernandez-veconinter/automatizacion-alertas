namespace TeamsNotificationService.Models;

public class TransactionSummary
{
    public DateTime FromTime { get; set; }
    public DateTime ToTime { get; set; }
    public List<TableSummary> Tables { get; set; } = [];
    public bool HasData => Tables.Any(t => t.TotalCount > 0);
}

public class TableSummary
{
    public string TableName { get; set; } = string.Empty;
    public List<GroupCount> Groups { get; set; } = [];
    public int TotalCount => Groups.Sum(g => g.Count);
}

public class GroupCount
{
    public string GroupKey { get; set; } = string.Empty;
    public int Count { get; set; }
}
