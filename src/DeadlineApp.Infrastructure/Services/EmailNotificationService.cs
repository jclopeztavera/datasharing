using DeadlineApp.Core.Interfaces;
using DeadlineApp.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;

namespace DeadlineApp.Infrastructure.Services;

public class EmailNotificationService : INotificationService
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly string _senderAddress;

    public EmailNotificationService(
        GraphServiceClient graphClient,
        IConfiguration configuration,
        ILogger<EmailNotificationService> logger)
    {
        _graphClient = graphClient;
        _logger = logger;
        _senderAddress = configuration["Notifications:SenderEmail"]
            ?? "no-reply@deadlineapp.com";
    }

    public async Task<NotificationResult> SendOverdueAlertAsync(
        DeadlineNotification deadline,
        string recipientEmail,
        string recipientName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = $"⚠ OVERDUE: {deadline.DeadlineTitle} — {deadline.MatterNumber}";
            var body = BuildOverdueAlertHtml(deadline, recipientName);

            var message = CreateMessage(subject, body, recipientEmail, recipientName, Importance.High);

            await _graphClient.Users[_senderAddress].SendMail
                .PostAsync(new SendMailPostRequestBody { Message = message, SaveToSentItems = false },
                    cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Sent overdue alert for deadline {DeadlineId} to {Email}",
                deadline.DeadlineId, recipientEmail);

            return NotificationResult.Succeeded("email");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send overdue alert for deadline {DeadlineId} to {Email}",
                deadline.DeadlineId, recipientEmail);
            return NotificationResult.Failed("email", ex.Message);
        }
    }

    public async Task<NotificationResult> SendDailySummaryAsync(
        AttorneyDeadlineSummary summary,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = BuildSummarySubject(summary);
            var body = BuildDailySummaryHtml(summary);

            var message = CreateMessage(
                subject, body, summary.AttorneyEmail, summary.AttorneyName,
                summary.OverdueDeadlines.Count > 0 ? Importance.High : Importance.Normal);

            await _graphClient.Users[_senderAddress].SendMail
                .PostAsync(new SendMailPostRequestBody { Message = message, SaveToSentItems = false },
                    cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Sent daily summary to {Email} — {Overdue} overdue, {AtRisk} at-risk, {Upcoming} upcoming",
                summary.AttorneyEmail, summary.OverdueDeadlines.Count,
                summary.AtRiskDeadlines.Count, summary.UpcomingDeadlines.Count);

            return NotificationResult.Succeeded("email");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send daily summary to {Email}", summary.AttorneyEmail);
            return NotificationResult.Failed("email", ex.Message);
        }
    }

    private static Message CreateMessage(
        string subject, string bodyHtml, string toEmail, string toName, Importance importance)
    {
        return new Message
        {
            Subject = subject,
            Importance = importance,
            Body = new ItemBody
            {
                ContentType = BodyType.Html,
                Content = bodyHtml,
            },
            ToRecipients = new List<Recipient>
            {
                new()
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = toEmail,
                        Name = toName,
                    }
                }
            }
        };
    }

    private static string BuildSummarySubject(AttorneyDeadlineSummary summary)
    {
        if (summary.OverdueDeadlines.Count > 0)
            return $"⚠ Daily Deadlines: {summary.OverdueDeadlines.Count} overdue, {summary.TotalCount} total — {summary.FirmName}";

        return $"Daily Deadline Summary: {summary.TotalCount} items — {summary.FirmName}";
    }

    internal static string BuildOverdueAlertHtml(DeadlineNotification d, string recipientName)
    {
        var daysOverdue = Math.Abs(d.DaysUntilDue);
        return $"""
            <html>
            <body style="font-family: Segoe UI, Arial, sans-serif; color: #333;">
              <h2 style="color: #d13438;">Overdue Deadline Alert</h2>
              <p>Hi {Escape(recipientName)},</p>
              <p>The following deadline is <strong>{daysOverdue} day(s) past due</strong>:</p>
              <table style="border-collapse: collapse; width: 100%; max-width: 600px;">
                <tr style="background: #fde7e9;">
                  <td style="padding: 8px; font-weight: bold;">Deadline</td>
                  <td style="padding: 8px;">{Escape(d.DeadlineTitle)}</td>
                </tr>
                <tr>
                  <td style="padding: 8px; font-weight: bold;">Matter</td>
                  <td style="padding: 8px;">{Escape(d.MatterNumber)} — {Escape(d.MatterTitle)}</td>
                </tr>
                <tr style="background: #fde7e9;">
                  <td style="padding: 8px; font-weight: bold;">Due Date</td>
                  <td style="padding: 8px;">{d.DueDate:MMMM d, yyyy}</td>
                </tr>
                {(d.CourtRuleCitation != null ? $"""
                <tr>
                  <td style="padding: 8px; font-weight: bold;">Rule</td>
                  <td style="padding: 8px;">{Escape(d.CourtRuleCitation)}</td>
                </tr>
                """ : "")}
              </table>
              <p style="margin-top: 16px;">Please take immediate action or mark this deadline as completed if already addressed.</p>
              <p style="color: #666; font-size: 12px;">This is an automated notification from Deadline Reliability App.</p>
            </body>
            </html>
            """;
    }

    internal static string BuildDailySummaryHtml(AttorneyDeadlineSummary summary)
    {
        var sections = new List<string>();

        if (summary.OverdueDeadlines.Count > 0)
            sections.Add(BuildDeadlineSection("Overdue", "#d13438", "#fde7e9", summary.OverdueDeadlines));

        if (summary.AtRiskDeadlines.Count > 0)
            sections.Add(BuildDeadlineSection("At Risk (Buffer)", "#ca5010", "#fff4ce", summary.AtRiskDeadlines));

        if (summary.UpcomingDeadlines.Count > 0)
            sections.Add(BuildDeadlineSection("Upcoming (Next 7 Days)", "#0078d4", "#deecf9", summary.UpcomingDeadlines));

        return $"""
            <html>
            <body style="font-family: Segoe UI, Arial, sans-serif; color: #333;">
              <h2>Daily Deadline Summary</h2>
              <p>Hi {Escape(summary.AttorneyName)},</p>
              <p>Here is your deadline summary for <strong>{summary.GeneratedAtUtc:MMMM d, yyyy}</strong>:</p>
              <p>
                <span style="color: #d13438; font-weight: bold;">{summary.OverdueDeadlines.Count} overdue</span> &middot;
                <span style="color: #ca5010; font-weight: bold;">{summary.AtRiskDeadlines.Count} at risk</span> &middot;
                <span style="color: #0078d4; font-weight: bold;">{summary.UpcomingDeadlines.Count} upcoming</span>
              </p>
              {string.Join("\n", sections)}
              {(summary.TotalCount == 0 ? "<p style=\"color: #107c10; font-weight: bold;\">✓ No pending deadlines. You're all clear!</p>" : "")}
              <p style="color: #666; font-size: 12px; margin-top: 24px;">This is an automated notification from Deadline Reliability App.</p>
            </body>
            </html>
            """;
    }

    private static string BuildDeadlineSection(
        string title, string headerColor, string bgColor, List<DeadlineNotification> deadlines)
    {
        var rows = deadlines.Select(d =>
        {
            var daysText = d.IsOverdue
                ? $"<span style=\"color: #d13438; font-weight: bold;\">{Math.Abs(d.DaysUntilDue)}d overdue</span>"
                : $"{d.DaysUntilDue}d remaining";

            return $"""
                <tr>
                  <td style="padding: 6px 8px; border-bottom: 1px solid #e0e0e0;">{Escape(d.MatterNumber)}</td>
                  <td style="padding: 6px 8px; border-bottom: 1px solid #e0e0e0;">{Escape(d.DeadlineTitle)}</td>
                  <td style="padding: 6px 8px; border-bottom: 1px solid #e0e0e0;">{d.DueDate:MMM d, yyyy}</td>
                  <td style="padding: 6px 8px; border-bottom: 1px solid #e0e0e0;">{daysText}</td>
                </tr>
                """;
        });

        return $"""
            <h3 style="color: {headerColor}; margin-top: 20px;">{title} ({deadlines.Count})</h3>
            <table style="border-collapse: collapse; width: 100%; max-width: 700px;">
              <tr style="background: {bgColor};">
                <th style="padding: 6px 8px; text-align: left;">Matter</th>
                <th style="padding: 6px 8px; text-align: left;">Deadline</th>
                <th style="padding: 6px 8px; text-align: left;">Due Date</th>
                <th style="padding: 6px 8px; text-align: left;">Status</th>
              </tr>
              {string.Join("\n", rows)}
            </table>
            """;
    }

    private static string Escape(string input) =>
        System.Net.WebUtility.HtmlEncode(input);
}
