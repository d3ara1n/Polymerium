using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Polymerium.Avalonia.Models;
using Sentry;

namespace Polymerium.Avalonia.Services;

public class FeedbackService
{
    public const int MAX_MESSAGE_LENGTH = 5000;
    public const int MAX_EMAIL_LENGTH = 254;

    private DateTimeOffset _lastSubmission;

    public FeedbackDraftModel Draft { get; } = new();

    public SubmissionResult Submit(string source, out FeedbackReceiptModel? receipt)
    {
        receipt = null;
        if (!SentrySdk.IsEnabled)
        {
            return SubmissionResult.Unavailable;
        }

        if (DateTimeOffset.UtcNow - _lastSubmission < TimeSpan.FromSeconds(30))
        {
            return SubmissionResult.TooFrequent;
        }

        // NOTE: An explicit scope keeps feedback separate from automatic breadcrumbs and attachments.
        var scope = new Scope(new() { DisableFileWrite = true, MaxBreadcrumbs = 0 });
        scope.SetTag("feedback.kind", Draft.Kind.ToString().ToLowerInvariant());
        scope.SetTag("feedback.source", source);
        scope.SetTag("feedback.language", CultureInfo.CurrentUICulture.Name);
        var feedback = new SentryFeedback(Draft.Message.Trim(),
            contactEmail: string.IsNullOrWhiteSpace(Draft.Email) ? null : Draft.Email.Trim());
        var id = SentrySdk.CaptureFeedback(feedback, out var result, scope);
        if (result != CaptureFeedbackResult.Success || id == SentryId.Empty)
        {
            return SubmissionResult.Failed;
        }

        _lastSubmission = DateTimeOffset.UtcNow;
        receipt = new(id.ToString());
        Draft.Message = string.Empty;
        Draft.Email = string.Empty;
        Draft.Kind = FeedbackKind.Problem;
        return SubmissionResult.Queued;
    }

    internal static SentryEvent PrepareEvent(SentryEvent original)
    {
        // NOTE: SDK processors enrich feedback too; only the fields disclosed in the form leave the app.
        var feedback = new SentryEvent
        {
            Level = SentryLevel.Info,
            Release = Program.Version,
            Environment = Program.Environment,
            Contexts = { Feedback = original.Contexts.Feedback }
        };
        feedback.SetTag("feedback.platform", RuntimeInformation.OSDescription);
        foreach (var key in new[] { "feedback.kind", "feedback.source", "feedback.language" })
        {
            if (original.Tags.TryGetValue(key, out var value))
            {
                feedback.SetTag(key, value);
            }
        }

        return feedback;
    }

    public enum SubmissionResult
    {
        Queued,
        Unavailable,
        TooFrequent,
        Failed
    }
}
