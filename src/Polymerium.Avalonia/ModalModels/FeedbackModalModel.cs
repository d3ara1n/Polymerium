using System;
using System.Collections.Generic;
using System.Net.Mail;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Mvvm.Activation;
using Microsoft.Extensions.Logging;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;

namespace Polymerium.Avalonia.ModalModels;

public partial class FeedbackModalModel(
    IViewContext<string> context,
    FeedbackService feedbackService,
    ILogger<FeedbackModalModel> logger) : ViewModelBase
{
    public FeedbackDraftModel Draft { get; } = feedbackService.Draft;
    public IReadOnlyList<FeedbackKind> Kinds { get; } = Enum.GetValues<FeedbackKind>();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    public partial FeedbackReceiptModel? Receipt { get; private set; }

    [ObservableProperty]
    public partial string? MessageErrorKey { get; private set; }

    [ObservableProperty]
    public partial string? EmailErrorKey { get; private set; }

    [ObservableProperty]
    public partial string? SubmissionErrorKey { get; private set; }

    private bool CanSubmit() => Receipt is null;

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private void Submit()
    {
        MessageErrorKey = string.IsNullOrWhiteSpace(Draft.Message)
                      || Draft.Message.Length > FeedbackService.MAX_MESSAGE_LENGTH
            ? nameof(LanguageManager.Keys.FeedbackModal_MessageError)
            : null;
        var email = Draft.Email?.Trim() ?? string.Empty;
        EmailErrorKey = email.Length > 0
                     && (email.Length > FeedbackService.MAX_EMAIL_LENGTH
                      || !MailAddress.TryCreate(email, out var address)
                      || !string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase)
                      || !address.Host.Contains('.'))
            ? nameof(LanguageManager.Keys.FeedbackModal_EmailError)
            : null;
        SubmissionErrorKey = null;
        if (MessageErrorKey is not null || EmailErrorKey is not null)
        {
            return;
        }

        try
        {
            var result = feedbackService.Submit(context.Parameter ?? "menu", out var receipt);
            switch (result)
            {
                case FeedbackService.SubmissionResult.Queued:
                    Receipt = receipt;
                    break;
                case FeedbackService.SubmissionResult.Unavailable:
                    SubmissionErrorKey = nameof(LanguageManager.Keys.FeedbackModal_UnavailableError);
                    break;
                case FeedbackService.SubmissionResult.TooFrequent:
                    SubmissionErrorKey = nameof(LanguageManager.Keys.FeedbackModal_TooFrequentError);
                    break;
                default:
                    SubmissionErrorKey = nameof(LanguageManager.Keys.FeedbackModal_SubmissionError);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not queue user feedback");
            SubmissionErrorKey = nameof(LanguageManager.Keys.FeedbackModal_SubmissionError);
        }
    }
}
