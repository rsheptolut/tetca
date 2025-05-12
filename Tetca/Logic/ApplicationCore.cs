using Tetca.Windows.NotifyIconMenu;

namespace Tetca.Logic
{
    /// <summary>
    /// Root service that is required for startup
    /// </summary>
    public record ApplicationCore(MainLoop MainLoop, NotifyIconLogic NotifyIconLogic, MainWindow MainWindow);
}
