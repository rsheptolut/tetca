using System.Threading.Tasks;

namespace Tetca.Notifiers.Speech
{
    /// <summary>
    /// ISpeech interface defines the contract for speech synthesis notifications.
    /// </summary>
    public interface ISpeech
    {
        /// <summary>
        /// Speaks the given message on the sound device, resetting to default playback device if no call is detected.
        /// </summary>
        void SpeakOnSoundDevice(string text);
    }
}