using Tetca.ActivityDetectors;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;

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
        /// <summary>
        /// Speaks the given message using the system's speech synthesizer.
        /// </summary>
        public Task Speak(string message)
        {
            try
            {
                SpeechSynthesizer synth = new SpeechSynthesizer();
                synth.SetOutputToDefaultAudioDevice();
                var voices = synth.GetInstalledVoices(new System.Globalization.CultureInfo(settings.VoiceNotificationCulture ?? "en-US"));
                var voice = voices.FirstOrDefault(v => v.VoiceInfo.Gender == VoiceGender.Female);

                var pb = new PromptBuilder();
                pb.StartVoice(voice.VoiceInfo.Name);
                pb.StartStyle(new PromptStyle(PromptEmphasis.None));
                pb.StartStyle(new PromptStyle(PromptVolume.ExtraLoud));

                pb.AppendText(message);
                pb.EndStyle();
                pb.EndStyle();
                pb.EndVoice();
                synth.SpeakAsync(pb);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while attempting to speak the message.");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Speaks the given message on the sound device, resetting to default playback device if no call is detected.
        /// </summary>
        public async Task SpeakOnSoundDevice(string message)
        {
            callDetector.Detect();
            if (!callDetector.IsActive)
            {
                await soundDeviceManager.ResetToDefaultPlaybackDevice(settings.VoiceNotificationSoundDevice);
            }

            _ = this.Speak(message);
        }
    }
}