let dotNetHelper = null;
let mediaRecorder = null;
let audioChunks = [];
let recordingInterval = null;
let recordingStartTime = null;
let audioContext = null;
let analyser = null;
let microphone = null;


window.initAudioRecorder = (dotNetReference) => {
    dotNetHelper = dotNetReference;
    console.log('Audio recorder initialized');
};

window.startRecording = async () => {
    try {
        console.log('Requesting microphone access...');
        const stream = await navigator.mediaDevices.getUserMedia({
            audio: {
                echoCancellation: true,
                noiseSuppression: true,
                autoGainControl: true
            }
        });

        console.log('Microphone access granted');

        // Check if browser supports webm
        let mimeType = 'audio/webm';
        if (!MediaRecorder.isTypeSupported(mimeType)) {
            mimeType = 'audio/mp4';
            console.log('WebM not supported, using MP4');
        }

        const options = { mimeType: mimeType };
        mediaRecorder = new MediaRecorder(stream, options);
        audioChunks = [];

        mediaRecorder.ondataavailable = (event) => {
            if (event.data.size > 0) {
                audioChunks.push(event.data);
                console.log('Audio chunk recorded:', event.data.size, 'bytes');
            }
        };

        setupAudioLevelMonitoring(stream);
        mediaRecorder.start();
        recordingStartTime = Date.now();

        console.log('Recording started');

        // Update timer every second
        recordingInterval = setInterval(() => {
            const elapsed = Math.floor((Date.now() - recordingStartTime) / 1000);
            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('UpdateRecordingTime', elapsed);
            }
        }, 1000);

        return true;
    } catch (error) {
        console.error('Error starting recording:', error);
        alert('Fehler beim Zugriff auf das Mikrofon. Bitte überprüfen Sie die Berechtigungen.\n\nFehler: ' + error.message);
        return false;
    }
};

window.stopRecording = () => {
    console.log('Stopping recording...');
    return new Promise((resolve) => {
        if (mediaRecorder && mediaRecorder.state !== 'inactive') {
            mediaRecorder.onstop = async () => {
                console.log('Recording stopped, processing audio...');
                const mimeType = mediaRecorder.mimeType || 'audio/webm';
                const audioBlob = new Blob(audioChunks, { type: mimeType });
                console.log('Audio blob created:', audioBlob.size, 'bytes');

                const base64Audio = await blobToBase64(audioBlob);
                console.log('Audio converted to base64, length:', base64Audio.length);

                // Cleanup
                clearInterval(recordingInterval);
                if (audioContext) {
                    audioContext.close();
                }
                if (microphone) {
                    microphone.getTracks().forEach(track => {
                        track.stop();
                        console.log('Microphone track stopped');
                    });
                }

                resolve(base64Audio);
            };

            mediaRecorder.stop();
        } else {
            console.log('No active recording to stop');
            resolve(null);
        }
    });
};

function setupAudioLevelMonitoring(stream) {
    // Temporarily disabled - was causing performance issues
    return;
}

function blobToBase64(blob) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onloadend = () => {
            const base64String = reader.result.split(',')[1];
            resolve(base64String);
        };
        reader.onerror = reject;
        reader.readAsDataURL(blob);
    });
}
