namespace QuickConvert
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
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
                await vm.SaveAppSettingsAsync().ConfigureAwait(true);
                if (vm.IsBusy)
                {
                    if (MessageBox.Show("Conversion en cours. Fermer quand-même ?", "Conversion en cours", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                    }
                }
            }
        }

        private async void Window_SourceInitialized(object sender, System.EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                await vm.LoadAppSettingsAsync().ConfigureAwait(true);
                if(Environment.GetCommandLineArgs().Length > 0)
                {
                    vm.AddStartupDestFileOrFolder(Environment.GetCommandLineArgs());
                }
                await LaunchConversionOnStartupAsync(vm).ConfigureAwait(true);
            }
        }

        private static async Task LaunchConversionOnStartupAsync(MainViewModel vm)
        {
            if (vm.DestFiles.Any() && Directory.Exists(vm.DestFolder))
            {
                await vm.Convert.ExecuteAsync().ConfigureAwait(true);
            }
        }

#pragma warning restore VSTHRD100 // Avoid async void methods (this is an event)
    }
}