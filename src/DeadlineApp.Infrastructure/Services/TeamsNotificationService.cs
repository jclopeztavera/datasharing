using System.Net.Http.Json;
using System.Text.Json;
using DeadlineApp.Core.Interfaces;
using DeadlineApp.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DeadlineApp.Infrastructure.Services;

/// <summary>
/// Sends notifications via Microsoft Teams incoming webhooks.
/// Configure "Notifications:TeamsWebhookUrl" in app settings.
/// </summary>
public class TeamsNotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TeamsNotificationService> _logger;
    private readonly string? _webhookUrl;

    public TeamsNotificationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TeamsNotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _webhookUrl = configuration["Notifications:TeamsWebhookUrl"];
    }

    public async Task<NotificationResult> SendOverdueAlertAsync(
        DeadlineNotification deadline,
        string recipientEmail,
        string recipientName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_webhookUrl))
        {
            _logger.LogDebug("Teams webhook URL not configured; skipping overdue alert");
            return NotificationResult.Failed("teams", "Webhook URL not configured");
        }

        try
        {
            var daysOverdue = Math.Abs(deadline.DaysUntilDue);
            var card = BuildAdaptiveCard(
                title: $"⚠ OVERDUE: {deadline.DeadlineTitle}",
                color: "attention",
                facts: new Dictionary<string, string>
                {
                    ["Matter"] = $"{deadline.MatterNumber} — {deadline.MatterTitle}",
                    ["Due Date"] = deadline.DueDate.ToString("MMMM d, yyyy"),
                    ["Days Overdue"] = daysOverdue.ToString(),
                    ["Assigned To"] = recipientName,
                },
                citation: deadline.CourtRuleCitation);

            await PostCardAsync(card, cancellationToken);

            _logger.LogInformation(
                "Sent Teams overdue alert for deadline {DeadlineId}", deadline.DeadlineId);

            return NotificationResult.Succeeded("teams");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send Teams overdue alert for deadline {DeadlineId}", deadline.DeadlineId);
            return NotificationResult.Failed("teams", ex.Message);
        }
    }

    public async Task<NotificationResult> SendDailySummaryAsync(
        AttorneyDeadlineSummary summary,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_webhookUrl))
        {
            _logger.LogDebug("Teams webhook URL not configured; skipping daily summary");
            return NotificationResult.Failed("teams", "Webhook URL not configured");
        }

        try
        {
            var facts = new Dictionary<string, string>
            {
                ["Attorney"] = summary.AttorneyName,
                ["Overdue"] = summary.OverdueDeadlines.Count.ToString(),
                ["At Risk"] = summary.AtRiskDeadlines.Count.ToString(),
                ["Upcoming (7 days)"] = summary.UpcomingDeadlines.Count.ToString(),
            };

            var title = summary.OverdueDeadlines.Count > 0
                ? $"⚠ Daily Summary: {summary.OverdueDeadlines.Count} overdue — {summary.FirmName}"
                : $"Daily Summary: {summary.TotalCount} deadlines — {summary.FirmName}";

            var card = BuildAdaptiveCard(
                title: title,
                color: summary.OverdueDeadlines.Count > 0 ? "attention" : "default",
                facts: facts);

            // Append overdue items as a compact list
            if (summary.OverdueDeadlines.Count > 0)
            {
                var overdueItems = summary.OverdueDeadlines
                    .Take(10)
                    .Select(d => $"- **{d.MatterNumber}**: {d.DeadlineTitle} (due {d.DueDate:MMM d})");

                card["attachments"]![0]!["content"]!["body"]!.AsArray().Add(
                    JsonSerializer.SerializeToNode(new
                    {
                        type = "TextBlock",
                        text = "**Overdue:**\n" + string.Join("\n", overdueItems),
                        wrap = true,
                        size = "small"
                    }));
            }

            await PostCardAsync(card, cancellationToken);

            _logger.LogInformation(
                "Sent Teams daily summary for {Attorney}", summary.AttorneyEmail);

            return NotificationResult.Succeeded("teams");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send Teams daily summary for {Attorney}", summary.AttorneyEmail);
            return NotificationResult.Failed("teams", ex.Message);
        }
    }

    private async Task PostCardAsync(JsonDocument card, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            _webhookUrl, card, cancellationToken: cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static JsonDocument BuildAdaptiveCard(
        string title,
        string color,
        Dictionary<string, string> facts,
        string? citation = null)
    {
        var factItems = facts.Select(kv => new { title = kv.Key, value = kv.Value }).ToList();

        var bodyItems = new List<object>
        {
            new
            {
                type = "TextBlock",
                text = title,
                weight = "bolder",
                size = "medium",
                color,
                wrap = true
            },
            new
            {
                type = "FactSet",
                facts = factItems
            }
        };

        if (citation != null)
        {
            bodyItems.Add(new
            {
                type = "TextBlock",
                text = $"Rule: {citation}",
                wrap = true,
                size = "small",
                color = "accent"
            });
        }

        var payload = new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    content = new
                    {
                        type = "AdaptiveCard",
                        version = "1.4",
                        body = bodyItems,
                        schema = "http://adaptivecards.io/schemas/adaptive-card.json"
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        return JsonDocument.Parse(json);
    }
}
