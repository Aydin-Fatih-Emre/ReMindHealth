using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using ReMindHealth.Services.Interfaces;

namespace ReMindHealth.Components.Pages
{
    public partial class Record
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private NotificationService NotificationService { get; set; } = default!;
        [Inject] private IConversationService ConversationService { get; set; } = default!;
        [Inject] private IServiceScopeFactory ScopeFactory { get; set; } = default!;

        // Recording state
        private bool isRecording = false;
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

                }
                catch (Exception ex)

                {
                    Console.WriteLine($"ERROR in OnAfterRenderAsync: {ex.Message}");
                }
            }
        }
        private async Task ToggleRecording()
        {
            if (!isRecording)
            {
                await JS.InvokeVoidAsync("startRecording");
                isRecording = true;
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

                var audioBytes = Convert.FromBase64String(audioData);

                var conversation = await ConversationService.CreateConversationWithAudioAsync(

                    noteText,

                    audioBytes

                );
                pendingConversationId = conversation.ConversationId;
                NotificationService.Notify(NotificationSeverity.Info, "Transkribiere...", "Bitte warten...");

                _ = TranscribeAudioBackgroundAsync(conversation.ConversationId);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProcessRecording] Error: {ex.Message}");
                NotificationService.Notify(NotificationSeverity.Error, "Fehler", $"Fehler bei der Verarbeitung: {ex.Message}");
            }

        }

        private async Task TranscribeAudioBackgroundAsync(Guid conversationId)
        {
            try
            {
                using var scope = ScopeFactory.CreateScope();
                var convSvc = scope.ServiceProvider.GetRequiredService<IConversationService>();
                for (int i = 0; i < 5; i++)
                {
                    await Task.Delay(1000);

                    var conversation = await convSvc.GetConversationAsync(conversationId);
                    if (conversation == null)

                    {

                        break;

                    }
                    if (conversation.ProcessingStatus == "Transcribed" ||

                        (conversation.ProcessingStatus == "Completed" && !string.IsNullOrEmpty(conversation.TranscriptionText)))

                    {
                        await InvokeAsync(() =>

                        {

                            transcriptionText = conversation.TranscriptionText ?? "";

                            showTranscriptionReview = true;

                            StateHasChanged();

                        });
                        await InvokeAsync(() =>

                            NotificationService.Notify(NotificationSeverity.Success, "Transkription fertig",

                                "Bitte überprüfen Sie den Text vor der Verarbeitung"));
                        break;

                    }
                    else if (conversation.ProcessingStatus == "Failed")
                    {
                        Console.WriteLine($"[TranscribeAudioBackgroundAsync] Failed: {conversation.ProcessingError}");

                        await InvokeAsync(() =>

                            NotificationService.Notify(NotificationSeverity.Error, "Fehler",

                                conversation.ProcessingError ?? "Transkription fehlgeschlagen"));

                        break;
                    }
                }
            }
            catch (Exception ex)

            {
                Console.WriteLine($"[TranscribeAudioBackgroundAsync] Exception: {ex.Message}");
                await InvokeAsync(() =>
                    NotificationService.Notify(NotificationSeverity.Error, "Fehler", ex.Message));
            }
        }

        private async Task StartProcessing()
        {
            if (!pendingConversationId.HasValue) return;

            showTranscriptionReview = false;
            StateHasChanged();

            try
            {
                var originalConversation = await ConversationService.GetConversationAsync(pendingConversationId.Value);
                if (originalConversation != null && originalConversation.TranscriptionText != transcriptionText)
                {
                    await ConversationService.UpdateTranscriptionTextOnlyAsync(
                        pendingConversationId.Value,
                        transcriptionText);

                    await Task.Delay(500);
                }

                await ConversationService.ContinueProcessingFromTranscriptionAsync(pendingConversationId.Value);

                NotificationService.Notify(NotificationSeverity.Success, "Verarbeitung gestartet",
                    "Wird im Hintergrund verarbeitet");
            }
            catch (Exception ex)
            {   
                Console.WriteLine($"[StartProcessing] Error: {ex.Message}");
                NotificationService.Notify(NotificationSeverity.Error, "Fehler", ex.Message);
            }
        }
        private void CancelProcessing()
        {
            showTranscriptionReview = false;
            transcriptionText = "";
            pendingConversationId = null;
            StateHasChanged();
            NotificationService.Notify(NotificationSeverity.Info, "Abgebrochen", "Verarbeitung wurde abgebrochen");
        }
    
        public void Dispose()

        {

            objRef?.Dispose();

        }
    }
}