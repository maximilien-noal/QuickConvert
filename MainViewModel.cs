namespace QuickConvert
{
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;

    public class BindableFileInfo : ObservableObject
    {
        private string _name = "";

        public string Name { get => _name; set { Set(nameof(Name), ref _name, value); } }
    }

    public class MainViewModel : ViewModelBase
    {
        private string _format = "MP3";

        public string Format { get => _format; set { Set(nameof(Format), ref _format, value); } }

        private ObservableCollection<string> _formats = new ObservableCollection<string>(new string[] { "MP3" });

        public ObservableCollection<string> Formats { get => _formats; internal set { Set(nameof(Formats), ref _formats, value); } }

        private int _bitrate = 224;

        public int Bitrate { get => _bitrate; set { Set(nameof(Bitrate), ref _bitrate, value); } }

        private ObservableCollection<int> _bitrates = new ObservableCollection<int>(new int[] { 224, 256, 320 });

        public ObservableCollection<int> Bitrates { get => _bitrates; internal set {Set(nameof(Bitrates), ref _bitrates, value); } }

        private bool _isNotOccupied = true;

        public bool IsNotOccupied { get => _isNotOccupied; set { Set(nameof(IsNotOccupied), ref _isNotOccupied, value); } }

        private string _extendedStatus = "";

        public string ExtendedStatus { get => _extendedStatus; set { Set(nameof(ExtendedStatus), ref _extendedStatus, value); } }

        private string _currentFileBeingProcessed = "";

        public string CurrentFileBeingProcessed { get => _currentFileBeingProcessed; set { Set(nameof(CurrentFileBeingProcessed), ref _currentFileBeingProcessed, value); } }

        private ObservableCollection<BindableFileInfo> _sourceFiles = new ObservableCollection<BindableFileInfo>();

        public ObservableCollection<BindableFileInfo> SourceFiles { get => _sourceFiles; internal set { Set(nameof(SourceFiles), ref _sourceFiles, value); } }

        private ObservableCollection<BindableFileInfo> _destFiles = new ObservableCollection<BindableFileInfo>();

        public ObservableCollection<BindableFileInfo> DestFiles { get => _destFiles; internal set { Set(nameof(DestFiles), ref _destFiles, value); } }

        private string _destFolder = "";

        public string DestFolder { get => _destFolder; set { Set(nameof(DestFolder), ref _destFolder, value); } }

        public RelayCommand Close { get; internal set; } = new RelayCommand(() => Application.Current.MainWindow.Close());

        public RelayCommand Convert { get; internal set; } = new RelayCommand(ConvertMethod);

        private static void ConvertMethod()
        {
            throw new NotImplementedException();
        }

        public RelayCommand PickDestFolder { get; internal set; } = new RelayCommand(PickDestFolderMethod);

        private static void PickDestFolderMethod()
        {
            throw new NotImplementedException();
        }

        public MainViewModel()
        {

        }
    }
}
