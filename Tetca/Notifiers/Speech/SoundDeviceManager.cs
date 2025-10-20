using System;
using System.Linq;
using System.Threading.Tasks;
using CoreAudio;

namespace Tetca.Notifiers.Speech
{
    /// <summary>
    /// SoundDeviceManager class is responsible for managing sound devices, specifically resetting to the default playback device.
    /// </summary>
    public class SoundDeviceManager(string voiceNotificationSoundDevice)
    {
        private PreviousDevice previousDevice;

        /// <summary>
        /// Resets the default playback device to a specific device (e.g., Realtek) and sets its volume and mute state.
        /// </summary>
        public void ResetToDefaultPlaybackDevice()
        {
            var enumerator = new MMDeviceEnumerator();
            var deviceCollection = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);

            var devices = Enumerable.Range(0, deviceCollection.Count).Select(index => deviceCollection[index]).ToList();

            var defaultDevice = devices.Where(x => x.DeviceFriendlyName.Contains(voiceNotificationSoundDevice) && x.DataFlow == DataFlow.Render).FirstOrDefault();

            if (defaultDevice != null)
            {
                PreviousDevice previousDevice = null;

                var selectedDevice = devices.FirstOrDefault(d => d.Selected);
                if (selectedDevice != null)
                {
                    previousDevice = new PreviousDevice
                    {
                        Name = selectedDevice.DeviceFriendlyName,
                        Volume = selectedDevice.AudioEndpointVolume.MasterVolumeLevelScalar,
                        IsMuted = selectedDevice.AudioEndpointVolume.Mute,
                    };
                }

                if (previousDevice != null)
                {
                    if (previousDevice.Name != defaultDevice.DeviceFriendlyName ||
                        previousDevice.Volume != defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar ||
                        previousDevice.IsMuted != defaultDevice.AudioEndpointVolume.Mute)
                    {
                        this.previousDevice = previousDevice;
                    }
                }

                defaultDevice.Selected = true;
                defaultDevice.AudioEndpointVolume.Mute = false;
                defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = 1;
            }
        }

        /// <summary>
        /// Resets the default playback device to a specific device (e.g., Realtek) and sets its volume and mute state.
        /// </summary>
        public void RestorePlaybackDevice()
        {
            if (this.previousDevice == null)
            {
                return;
            }

            var enumerator = new MMDeviceEnumerator();
            var deviceCollection = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);

            var devices = Enumerable.Range(0, deviceCollection.Count).Select(index => deviceCollection[index]).ToList();
            var device = devices.Where(x => x.DeviceFriendlyName == this.previousDevice.Name && x.DataFlow == DataFlow.Render).FirstOrDefault();

            device.Selected = true;
            device.AudioEndpointVolume.Mute = this.previousDevice.IsMuted;
            device.AudioEndpointVolume.MasterVolumeLevelScalar = this.previousDevice.Volume;
        }

        public void Mute(bool mute, bool onlyIfDefaultDeviceIsSelected = true)
        {
            var enumerator = new MMDeviceEnumerator();
            var deviceCollection = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);

            var devices = Enumerable.Range(0, deviceCollection.Count).Select(index => deviceCollection[index]).ToList();

            var defaultDevice = devices.Where(x => x.DeviceFriendlyName.Contains(voiceNotificationSoundDevice) && x.DataFlow == DataFlow.Render).FirstOrDefault();

            var selectedDevice = devices.FirstOrDefault(d => d.Selected);

            var defaultDeviceIsSelected = selectedDevice == defaultDevice;

            if (defaultDeviceIsSelected || !onlyIfDefaultDeviceIsSelected)
            {
                selectedDevice.AudioEndpointVolume.Mute = mute;
            }
        }

        class PreviousDevice
        {
            public string Name { get; set; }
            public float Volume { get; set; }
            public bool IsMuted { get; set; }
        }
    }
}
