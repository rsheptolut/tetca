namespace Tetca.Windows.MainWindow
{
    /// <summary>
    /// ViewModel for the MainWindow, responsible for managing the debug state and providing data binding.
    /// </summary>
    public class MainWindowViewModel(DebugState debugState) : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        public void RefreshAll()
        {
            this.OnPropertyChanged(nameof(this.DebugState));
        }

        /// <summary>
        /// Gets or sets the debug state of the application.
        /// </summary>
        public DebugState DebugState { get; set; } = debugState;

        //private string test = "hello";

        //public string Test { get => this.test; set => this.SetProperty(ref this.test, value); }
    }
}
