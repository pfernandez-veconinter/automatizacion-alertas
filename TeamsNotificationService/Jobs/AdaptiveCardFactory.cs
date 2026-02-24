using TeamsNotificationService.Models;

namespace TeamsNotificationService.Jobs;

public static class AdaptiveCardFactory
{
    public static AdaptiveCardPayload CreateTransactionSummaryCard(TransactionSummary summary)
    {
        var fromStr = summary.FromTime.ToString("HH:mm");
        var toStr   = summary.ToTime.ToString("HH:mm");
        var dateStr = summary.ToTime.ToString("dddd, MMMM dd yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-ES"));

        var body = new List<CardElement>
        {
            new()
            {
                Type   = "TextBlock",
                Text   = "📊 Resumen de Transacciones",
                Size   = "Large",
                Weight = "Bolder",
                Color  = "Accent",
                Wrap   = true
            },
            new()
            {
                Type    = "TextBlock",
                Text    = $"📅 {dateStr}  |  🕐 {fromStr} – {toStr}",
                Wrap    = true,
                Spacing = "Small"
            },
            new()
            {
                Type      = "TextBlock",
                Text      = "---",
                Separator = true
            }
        };

        var facts = new List<CardFact>();
        foreach (var table in summary.Tables)
        {
            if (table.Groups.Count == 0)
            {
                facts.Add(new CardFact { Title = $"Pagos {table.TableName}", Value = "0" });
            }
            else
            {
                foreach (var group in table.Groups)
                {
                    facts.Add(new CardFact
                    {
                        Title = $"Pagos {table.TableName} {group.GroupKey.ToUpperInvariant()}",
                        Value = group.Count.ToString()
                    });
                }
            }
        }

        body.Add(new CardElement { Type = "FactSet", Facts = facts });

        return new AdaptiveCardPayload
        {
            Attachments =
            [
                new Attachment
                {
                    Content = new AdaptiveCard { Body = body }
                }
            ]
        };
    }

    public static AdaptiveCardPayload CreatePaymentLogSummaryCard(Models.PaymentLogSummary summary)
    {
        var fromStr = summary.FromTime.ToString("HH:mm");
        var toStr   = summary.ToTime.ToString("HH:mm");
        var dateStr = summary.ToTime.ToString("dddd, MMMM dd yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-ES"));

        var body = new List<CardElement>
        {
            new()
            {
                Type   = "TextBlock",
                Text   = "💳 Reporte de Pagos",
                Size   = "Large",
                Weight = "Bolder",
                Color  = "Accent",
                Wrap   = true
            },
            new()
            {
                Type    = "TextBlock",
                Text    = $"📅 {dateStr}  |  🕐 {fromStr} – {toStr}",
                Wrap    = true,
                Spacing = "Small"
            },
            new()
            {
                Type      = "TextBlock",
                Text      = "---",
                Separator = true
            }
        };

        // Processed payments section
        body.Add(new CardElement
        {
            Type   = "TextBlock",
            Text   = "✅ Pagos Procesados",
            Weight = "Bolder",
            Wrap   = true
        });

        var processedFacts = new List<CardFact>();
        if (summary.Processed.Count == 0)
        {
            processedFacts.Add(new CardFact { Title = "Total", Value = "0" });
        }
        else
        {
            foreach (var entry in summary.Processed)
            {
                processedFacts.Add(new CardFact
                {
                    Title = $"{entry.PaymentMethod.ToUpperInvariant()} - {entry.CountryId.ToUpperInvariant()}",
                    Value = entry.Count.ToString()
                });
            }
        }
        body.Add(new CardElement { Type = "FactSet", Facts = processedFacts });

        body.Add(new CardElement { Type = "TextBlock", Text = "---", Separator = true });

        // Not processed payments section
        body.Add(new CardElement
        {
            Type   = "TextBlock",
            Text   = "⏳ Pagos No Procesados",
            Weight = "Bolder",
            Wrap   = true
        });

        var notProcessedFacts = new List<CardFact>();
        if (summary.NotProcessed.Count == 0)
        {
            notProcessedFacts.Add(new CardFact { Title = "Total", Value = "0" });
        }
        else
        {
            foreach (var entry in summary.NotProcessed)
            {
                notProcessedFacts.Add(new CardFact
                {
                    Title = $"{entry.PaymentMethod.ToUpperInvariant()} - {entry.CountryId.ToUpperInvariant()}",
                    Value = entry.Count.ToString()
                });
            }
        }
        body.Add(new CardElement { Type = "FactSet", Facts = notProcessedFacts });

        return new AdaptiveCardPayload
        {
            Attachments =
            [
                new Attachment
                {
                    Content = new AdaptiveCard { Body = body }
                }
            ]
        };
    }

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
