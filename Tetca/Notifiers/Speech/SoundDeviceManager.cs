using System.Linq;
using System.Threading.Tasks;
using CoreAudio;

namespace Tetca.Notifiers.Speech
{
    /// <summary>
    /// SoundDeviceManager class is responsible for managing sound devices, specifically resetting to the default playback device.
    /// </summary>
    internal class SoundDeviceManager
    {
        /// <summary>
        /// Resets the default playback device to a specific device (e.g., Realtek) and sets its volume and mute state.
        /// </summary>
        public Task ResetToDefaultPlaybackDevice(string voiceNotificationSoundDevice)
        {
            var enumerator = new MMDeviceEnumerator();
            var deviceCollection = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);

            var devices = Enumerable.Range(0, deviceCollection.Count).Select(index => deviceCollection[index]).ToList();
            var device = devices.Where(x => x.DeviceFriendlyName.Contains(voiceNotificationSoundDevice) && x.DataFlow == DataFlow.Render).FirstOrDefault();

            device.AudioEndpointVolume.Mute = false;
            device.Selected = true;
            device.AudioEndpointVolume.MasterVolumeLevelScalar = 1;

            return Task.CompletedTask;
        }
    }
}
