using System;
using Tetca.Notifiers.Speech;

namespace Tetca.Logic
{
    /// <summary>
    /// Class that toggles Do Not Disturb mode based on work hours
    /// </summary>
    public class DoNotDisturb(ICurrentTime currentTime, Settings settings, SoundDeviceManager soundDeviceManager)
    {
        public bool IsOn { get; private set; }

        internal bool ShouldBeOn(DateTime time)
        {
            return time.TimeOfDay >= settings.WorkHoursFrom && time.TimeOfDay < settings.WorkHoursTo;
        }

        internal void ToggleIfNeeded()
        {
            var now = currentTime.Now;
            var shouldBeOn = ShouldBeOn(now);
            if (IsOn != shouldBeOn)
            {
                IsOn = shouldBeOn;
                UpdateSoundSettings();
            }
        }

        private void UpdateSoundSettings()
        {
            soundDeviceManager.Mute(IsOn, true);
        }
    }
}