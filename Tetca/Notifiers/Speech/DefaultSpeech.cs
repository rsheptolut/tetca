using Tetca.ActivityDetectors;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Threading;

namespace Tetca.Notifiers.Speech
{
    /// <summary>
    /// DefaultSpeech class is responsible for handling speech synthesis notifications, the default implementation of <see cref="ISpeech"/>
    /// </summary>
    internal class DefaultSpeech(
            Settings settings,
            CallDetector callDetector,
            SoundDeviceManager soundDeviceManager,
            ILogger<DefaultSpeech> logger) : ISpeech
    {
        private SpeechSynthesizer synth;
        private string voiceName;
        private bool initialized;
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public async Task Initialize()
        {
            if (this.initialized)
            {
                return;
            }

            await this.semaphore.WaitAsync();
            
            try
            {
                if (this.initialized)
                {
                    return;
                }

                this.InitializeInternal();
                this.initialized = true;

            }
            finally
            {
                this.semaphore.Release();
            }
        }

        public void InitializeInternal()
        {
            this.synth = new SpeechSynthesizer();
            this.synth.SetOutputToDefaultAudioDevice();
            var voices = this.synth.GetInstalledVoices(new System.Globalization.CultureInfo(settings.VoiceNotificationCulture ?? "en-US"));
            var voice = voices.FirstOrDefault(v => v.VoiceInfo.Gender == VoiceGender.Female);
            this.voiceName = voice.VoiceInfo.Name;
        }

        /// <summary>
        /// Speaks the given message using the system's speech synthesizer.
        /// </summary>
        public async Task SpeakAsync(string message)
        {
            await this.Initialize();

            try
            {
                var pb = new PromptBuilder();
                pb.StartVoice(this.voiceName);
                pb.StartStyle(new PromptStyle(PromptEmphasis.None));
                pb.StartStyle(new PromptStyle(PromptVolume.ExtraLoud));

                pb.AppendText(message);
                pb.EndStyle();
                pb.EndStyle();
                pb.EndVoice();
                var p = this.synth.SpeakAsync(pb);
                while (!p.IsCompleted)
                {
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while attempting to speak the message.");
            }
        }

        /// <summary>
        /// Speaks the given message on the sound device, resetting to default playback device if no call is detected.
        /// </summary>
        public void SpeakOnSoundDevice(string message)
        {
            _ = this.SpeakOnSoundDeviceAsync(message);
        }

        /// <summary>
        /// Speaks the given message on the sound device, resetting to default playback device if no call is detected.
        /// </summary>
        public async Task SpeakOnSoundDeviceAsync(string message)
        {
            callDetector.Detect();
            if (!callDetector.IsActive)
            {
                soundDeviceManager.ResetToDefaultPlaybackDevice();
            }

            await Task.Delay(100);
            await this.SpeakAsync(message);
            await Task.Delay(100);

            soundDeviceManager.RestorePlaybackDevice();
        }
    }
}