namespace QuickConvert
{
    using System.Windows;

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentUICulture.Name);
        }

#pragma warning disable VSTHRD100 // Avoid async void methods (thsi is an event)

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                if (vm.IsBusy)
                {
                    if (MessageBox.Show("Conversion en cours. Fermer quand-même ?", "Conversion en cours", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                    }
                }
                else
                {
                    await vm.SaveAppSettingsAsync().ConfigureAwait(true);
                }
            }
        }

        private async void Window_SourceInitialized(object sender, System.EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                await vm.LoadAppSettingsAsync().ConfigureAwait(true);
            }
        }

#pragma warning restore VSTHRD100 // Avoid async void methods (thsi is an event)
    }
}