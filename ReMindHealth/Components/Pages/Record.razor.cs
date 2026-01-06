using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Radzen;
using ReMindHealth.Application.Interfaces.IServices;

namespace ReMindHealth.Components.Pages
{
    public partial class Record : IDisposable
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IConversationService ConversationService { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        // Recording state
        private bool isRecording = false;
        private bool isProcessing = false;
        private string recordingDuration = "00:00";
        private string audioLevel = "Leise";
        private string noteText = "";
        private DotNetObjectReference<Record>? objRef;

        private bool showTranscriptionReview = false;
        private string transcriptionText = "";
        private Guid? pendingConversationId = null;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    objRef = DotNetObjectReference.Create(this);
                    await JS.InvokeVoidAsync("initAudioRecorder", objRef);
                    await SetupNavigationWarning();

                    NavigationManager.RegisterLocationChangingHandler(OnLocationChanging);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR in OnAfterRenderAsync: {ex.Message}");
                }
            }
        }

        private async Task SetupNavigationWarning()
        {
            await JS.InvokeVoidAsync("eval", @"
                window.addEventListener('beforeunload', function(e) {
                    if (window.hasTranscriptionReview === true) {
                        e.preventDefault();
                        e.returnValue = '';
                        return 'Sie haben eine ungespeicherte Transkription. Möchten Sie die Seite wirklich verlassen?';
                    }
                });
            ");
        }

        private async ValueTask OnLocationChanging(LocationChangingContext context)
        {
            if (showTranscriptionReview || isProcessing || isRecording)
            {
                string message = isRecording
                    ? "Sie nehmen gerade auf. Möchten Sie die Seite wirklich verlassen? Die Aufnahme geht verloren."
                    : "Sie haben eine laufende Verarbeitung. Möchten Sie die Seite wirklich verlassen? Alle Daten gehen verloren.";

                var confirmed = await JS.InvokeAsync<bool>("confirm", message);

                if (!confirmed)
                {
                    context.PreventNavigation();
                }
                else
                {
                    // Clean up state
                    showTranscriptionReview = false;
                    transcriptionText = "";
                    pendingConversationId = null;
                    isProcessing = false;
                    isRecording = false;
                    await JS.InvokeVoidAsync("eval", "window.hasTranscriptionReview = false;");
                }
            }
        }

        private async Task ToggleRecording()
        {
            if (isProcessing)
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Bitte warten",
                    "Eine Aufnahme wird bereits verarbeitet");
                return;
            }

            if (!isRecording)
            {
                await JS.InvokeVoidAsync("startRecording");
                isRecording = true;
                await JS.InvokeVoidAsync("eval", "window.hasTranscriptionReview = true;");
            }
            else
            {
                var audioData = await JS.InvokeAsync<string>("stopRecording");
                isRecording = false;
                await ProcessRecording(audioData);
            }
        }

        [JSInvokable]
        public void UpdateRecordingTime(int seconds)
        {
            var minutes = seconds / 60;
            var secs = seconds % 60;
            recordingDuration = $"{minutes:D2}:{secs:D2}";
            StateHasChanged();
        }

        [JSInvokable]
        public void UpdateAudioLevel(string level)
        {
            audioLevel = level;
            StateHasChanged();
        }

        private async Task ProcessRecording(string audioData)
        {
            try
            {
                if (string.IsNullOrEmpty(audioData))
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Fehler", "Keine Audio-Daten empfangen");
                    return;
                }

                isProcessing = true;
                await JS.InvokeVoidAsync("eval", "window.hasTranscriptionReview = true;");

                NotificationService.Notify(
                    NotificationSeverity.Info,
                    "Verarbeitung gestartet",
                    "Transkribiere Audio...",
                    duration: 4000);

                var audioBytes = Convert.FromBase64String(audioData);

                var conversation = await ConversationService.CreateConversationWithAudioAsync(
                    noteText,
                    audioBytes
                );

                pendingConversationId = conversation.ConversationId;

                if (conversation.ProcessingStatus == "Transcribed")
                {
                    transcriptionText = conversation.TranscriptionText ?? "";
                    showTranscriptionReview = true;

                    await JS.InvokeVoidAsync("eval", "window.hasTranscriptionReview = true;");

                    NotificationService.Notify(
                        NotificationSeverity.Success,
                        "✓ Transkription abgeschlossen!",
                        "Bitte überprüfen Sie den Text vor der Verarbeitung",
                        duration: 5000);

                    await JS.InvokeVoidAsync("console.log", "Transcription completed successfully");
                }
                else if (conversation.ProcessingStatus == "Failed")
                {
                    NotificationService.Notify(
                        NotificationSeverity.Error,
                        "✗ Transkription fehlgeschlagen",
                        conversation.ProcessingError ?? "Ein unbekannter Fehler ist aufgetreten",
                        duration: 8000);
                    isProcessing = false;
                    await JS.InvokeVoidAsync("eval", "window.hasTranscriptionReview = false;"); 
                }

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProcessRecording] Error: {ex.Message}");

                NotificationService.Notify(
                    NotificationSeverity.Error,
                    "✗ Fehler bei der Verarbeitung",
                    $"Details: {ex.Message}",
                    duration: 8000);

                isProcessing = false;
                await JS.InvokeVoidAsync("eval", "window.hasTranscriptionReview = false;"); 
                StateHasChanged();
            }
        }

        private async Task StartProcessing()
        {
            if (!pendingConversationId.HasValue) return;

            showTranscriptionReview = false;

            await JS.InvokeVoidAsync("eval", "window.hasTranscriptionReview = false;");

            StateHasChanged();

            try
            {
                NotificationService.Notify(
                    NotificationSeverity.Info,
                    "Analyse gestartet",
                    "Extrahiere Termine, Aufgaben und Notizen...",
                    duration: 4000);

                var originalConversation = await ConversationService.GetConversationAsync(pendingConversationId.Value);
                if (originalConversation != null && originalConversation.TranscriptionText != transcriptionText)
                {
                    await ConversationService.UpdateTranscriptionTextOnlyAsync(
                        pendingConversationId.Value,
                        transcriptionText);
                }

                await ConversationService.ContinueProcessingFromTranscriptionAsync(pendingConversationId.Value);

                NotificationService.Notify(
                    NotificationSeverity.Success,
                    "✓ Analyse läuft",
                    "Die Verarbeitung wird im Hintergrund fortgesetzt",
                    duration: 5000);

                // Reset state
                isProcessing = false;
                pendingConversationId = null;
                noteText = "";
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StartProcessing] Error: {ex.Message}");
                NotificationService.Notify(
                    NotificationSeverity.Error,
                    "Fehler",
                    ex.Message,
                    duration: 8000);
            }
        }

        private async Task CancelProcessing()
        {
            showTranscriptionReview = false;
            transcriptionText = "";
            pendingConversationId = null;
            isProcessing = false;

            await JS.InvokeVoidAsync("eval", "window.hasTranscriptionReview = false;");

            StateHasChanged();

            NotificationService.Notify(
                NotificationSeverity.Warning,
                "Abgebrochen",
                "Die Verarbeitung wurde abgebrochen",
                duration: 4000);
        }

        public void Dispose()
        {
            objRef?.Dispose();
        }
    }
}