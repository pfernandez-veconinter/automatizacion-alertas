using TeamsNotificationService.Models;

namespace TeamsNotificationService.Jobs;

public static class AdaptiveCardFactory
{
    public static AdaptiveCardPayload CreateScheduledNotification(string timeLabel, DateTime now)
    {
        var dateStr = now.ToString("dddd, MMMM dd yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-ES"));
        var timeStr = now.ToString("HH:mm");

        return new AdaptiveCardPayload
        {
            Attachments =
            [
                new Attachment
                {
                    Content = new AdaptiveCard
                    {
                        Body =
                        [
                            new CardElement
                            {
                                Type = "TextBlock",
                                Text = $"🔔 Notificación Programada - {timeLabel}",
                                Size = "Large",
                                Weight = "Bolder",
                                Color = "Accent",
                                Wrap = true
                            },
                            new CardElement
                            {
                                Type = "TextBlock",
                                Text = $"📅 {dateStr}  |  🕐 {timeStr}",
                                Wrap = true,
                                Spacing = "Small"
                            },
                            new CardElement
                            {
                                Type = "TextBlock",
                                Text = "---",
                                Separator = true
                            },
                            new CardElement
                            {
                                Type = "FactSet",
                                Facts =
                                [
                                    new CardFact { Title = "Estado", Value = "✅ Servicio activo" },
                                    new CardFact { Title = "Entorno", Value = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production" },
                                    new CardFact { Title = "Hora de envío", Value = timeStr }
                                ]
                            }
                        ]
                    }
                }
            ]
        };
    }
}
