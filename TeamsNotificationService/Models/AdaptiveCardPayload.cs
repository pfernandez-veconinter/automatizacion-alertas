namespace TeamsNotificationService.Models;

public class AdaptiveCardPayload
{
    public string Type { get; set; } = "message";
    public List<Attachment> Attachments { get; set; } = [];
}

public class Attachment
{
    public string ContentType { get; set; } = "application/vnd.microsoft.card.adaptive";
    public string ContentUrl { get; set; } = string.Empty;
    public AdaptiveCard Content { get; set; } = new();
}

public class AdaptiveCard
{
    public string Type { get; set; } = "AdaptiveCard";
    public string Version { get; set; } = "1.4";
    public List<CardElement> Body { get; set; } = [];
    public List<CardAction> Actions { get; set; } = [];
    public string Schema { get; set; } = "http://adaptivecards.io/schemas/adaptive-card.json";
    public string MsTeams { get; set; } = string.Empty;
}

public class CardElement
{
    public string Type { get; set; } = string.Empty;
    public string? Text { get; set; }
    public string? Size { get; set; }
    public string? Weight { get; set; }
    public string? Color { get; set; }
    public bool? Wrap { get; set; }
    public string? Spacing { get; set; }
    public string? Style { get; set; }
    public bool? Separator { get; set; }
    public List<CardColumn>? Columns { get; set; }
    public List<CardFact>? Facts { get; set; }
}

public class CardColumn
{
    public string Type { get; set; } = "Column";
    public string Width { get; set; } = "auto";
    public List<CardElement> Items { get; set; } = [];
}

public class CardFact
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class CardAction
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Data { get; set; }
}
