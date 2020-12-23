namespace QuickConvert
{
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;
    using GalaSoft.MvvmLight.Ioc;

    using Microsoft.Win32;

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;

    public class FileInfoViewModel : ViewModelBase
    {
        private string _fullFilePath = "";

        public FileInfo Info => new FileInfo(_fullFilePath);

        private string _name = "";

        public string Name { get => _name; set { Set(nameof(Name), ref _name, value); } }

        public RelayCommand RemoveSourceFile { get; internal set; }

        public FileInfoViewModel(string fullFilePath)
        {
            if(string.IsNullOrWhiteSpace(fullFilePath) || File.Exists(fullFilePath) == false)
            {
                throw new FileNotFoundException(fullFilePath);
            }
            _fullFilePath = fullFilePath;
            RemoveSourceFile = new RelayCommand(() => SimpleIoc.Default.GetInstance<MainViewModel>().RemoveSourceFile(this));
        }
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

        private ObservableCollection<FileInfoViewModel> _sourceFiles = new ObservableCollection<FileInfoViewModel>();

        public ObservableCollection<FileInfoViewModel> SourceFiles { get => _sourceFiles; internal set { Set(nameof(SourceFiles), ref _sourceFiles, value); } }

        private ObservableCollection<FileInfoViewModel> _destFiles = new ObservableCollection<FileInfoViewModel>();

        public ObservableCollection<FileInfoViewModel> DestFiles { get => _destFiles; internal set { Set(nameof(DestFiles), ref _destFiles, value); } }

        private string _destFolder = "";

        public string DestFolder { get => _destFolder; set { Set(nameof(DestFolder), ref _destFolder, value); } }

        public RelayCommand Close { get; internal set; } = new RelayCommand(() => Application.Current.MainWindow.Close());

        public RelayCommand Convert { get; internal set; }

        public RelayCommand PickSourceFiles { get; internal set; }

        private static void ConvertMethod()
        {
            throw new NotImplementedException();
        }

        public RelayCommand PickDestFolder { get; internal set; }

        private void PickDestFolderMethod()
        {
            var ofd = new OpenFileDialog();
            ofd.CheckPathExists = true;
            ofd.Title = "Dossier de destination...";
            ofd.InitialDirectory = Path.GetDirectoryName(_sourceFiles.FirstOrDefault()?.Info.FullName);
            if (ofd.ShowDialog() == true)
            {

            }
        }

        public MainViewModel()
        {
            PickDestFolder = new RelayCommand(PickDestFolderMethod);
            PickSourceFiles = new RelayCommand(PickSourceFilesMethod);
            Convert = new RelayCommand(ConvertMethod);
        }

        private void PickSourceFilesMethod()
        {
            throw new NotImplementedException();
        }

        public void RemoveSourceFile(FileInfoViewModel mainViewModel)
        {
            throw new NotImplementedException();
        }
    }
}
