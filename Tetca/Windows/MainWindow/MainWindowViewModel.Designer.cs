using System;
using Tetca.ActivityDetectors;
using Tetca.Logic;

namespace Tetca.Windows.MainWindow
{
    public class MainWindowViewModelDesigner
    {
        public DebugStateDesigner DebugState { get; set; } = new DebugStateDesigner();
    }

    public class DebugStateDesigner
    {
        public TimeSpan ActivityTime => TimeSpan.FromHours(0.3234255346456);

        public TimeSpan WorkedTodayNormie8h => TimeSpan.FromHours(1.55346545456456);

        public DateTime LastActivity => DateTime.Now;

        public TimeSpan CurrentIdleTime => TimeSpan.Zero;

        public bool CallDetected => true;

        public bool InputDetected => false;
    }
}
