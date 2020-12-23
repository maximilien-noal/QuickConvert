namespace QuickConvert
{
    using AsyncAwaitBestPractices.MVVM;

    using FFMpegCore;
    using FFMpegCore.Enums;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;
    using GalaSoft.MvvmLight.Ioc;

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Forms;

    using Application = System.Windows.Application;

    public class LogEntry : ObservableObject
    {
        private TaskStatus _finalStatus;

        public TaskStatus FinalStatus { get => _finalStatus; internal set { Set(nameof(FinalStatus), ref _finalStatus, value); } }

        private string _sourceFile = "";

        public string SourceFile { get => _sourceFile; internal set { Set(nameof(SourceFile), ref _sourceFile, value); } }

        private string _destFile = "";

        public string DestFile { get => _destFile; internal set { Set(nameof(DestFile), ref _destFile, value); } }
    }

    public class FileInfoViewModel : ViewModelBase
    {
        private readonly string _fullFilePath = "";

        public FileInfo Info => new FileInfo(_fullFilePath);

        private string _name = "";

        public string Name { get => _name; set { Set(nameof(Name), ref _name, value); } }

        private string _shortFileName = "";

        public string ShortFileName { get => _shortFileName; set { Set(nameof(ShortFileName), ref _shortFileName, value); } }

        public RelayCommand RemoveSourceFile { get; internal set; }

        public FileInfoViewModel(string fullFilePath)
        {
            if (string.IsNullOrWhiteSpace(fullFilePath) || File.Exists(fullFilePath) == false)
            {
                throw new FileNotFoundException(fullFilePath);
            }
            _fullFilePath = fullFilePath;
            RemoveSourceFile = new RelayCommand(() => SimpleIoc.Default.GetInstance<MainViewModel>().RemoveSourceFile(this));
        }
    }

    public class MainViewModel : ViewModelBase, IProgress<Tuple<int, string>>
    {
        private ObservableCollection<LogEntry> _logs = new ObservableCollection<LogEntry>();

        public ObservableCollection<LogEntry> Logs { get => _logs; internal set { Set(nameof(Logs), ref _logs, value); } }

        private int _shortFileNameCharLimit = 20;

        public int ShortFileNameCharLimit { get => _shortFileNameCharLimit; set { Set(nameof(ShortFileNameCharLimit), ref _shortFileNameCharLimit, value); } }

        private bool _useSourceFolderAsDest = true;

        public bool UseSourceFolderAsDest { get => _useSourceFolderAsDest; set { Set(nameof(UseSourceFolderAsDest), ref _useSourceFolderAsDest, value); } }

        private string _format = "MP3";

        public string Format { get => _format; set { Set(nameof(Format), ref _format, value); } }

        private ObservableCollection<string> _formats = new ObservableCollection<string>(new string[] { "MP3" });

        public ObservableCollection<string> Formats { get => _formats; internal set { Set(nameof(Formats), ref _formats, value); } }

        private int _bitrate = 224;

        public int Bitrate { get => _bitrate; set { Set(nameof(Bitrate), ref _bitrate, value); } }

        private ObservableCollection<int> _bitrates = new ObservableCollection<int>(new int[] { 224, 256, 320 });

        public ObservableCollection<int> Bitrates { get => _bitrates; internal set { Set(nameof(Bitrates), ref _bitrates, value); } }

        private bool _isBusy = false;

        public bool IsBusy { get => _isBusy; set { Set(nameof(IsBusy), ref _isBusy, value); } }

        private string _lastProcessedFile = "";

        public string LastProcessedFile { get => _lastProcessedFile; set { Set(nameof(LastProcessedFile), ref _lastProcessedFile, value); } }

        private ObservableCollection<FileInfoViewModel> _sourceFiles = new ObservableCollection<FileInfoViewModel>();

        public ObservableCollection<FileInfoViewModel> SourceFiles { get => _sourceFiles; internal set { Set(nameof(SourceFiles), ref _sourceFiles, value); } }

        private ObservableCollection<FileInfoViewModel> _destFiles = new ObservableCollection<FileInfoViewModel>();

        public ObservableCollection<FileInfoViewModel> DestFiles { get => _destFiles; internal set { Set(nameof(DestFiles), ref _destFiles, value); } }

        private string _destFolder = "";

        public string DestFolder { get => _destFolder; set { Set(nameof(DestFolder), ref _destFolder, value); } }

        public RelayCommand Close { get; internal set; } = new RelayCommand(() => Application.Current.MainWindow.Close());

        public AsyncCommand Convert { get; internal set; }

        public RelayCommand PickSourceFiles { get; internal set; }

        public RelayCommand DeleteAllSourceFiles { get; internal set; }

        private async Task ConvertMethodAsync()
        {
            IsBusy = true;
            Percentage = 0;
            LastProcessedFile = "";
            try
            {
                var tasks = new List<Task<bool>>();
                for (int i = 0; i < SourceFiles.Count; i++)
                {
                    FileInfoViewModel? sourcefile = SourceFiles[i];
                    if (File.Exists(sourcefile.Info.FullName))
                    {
                        string subFolder = Path.Combine(Path.GetDirectoryName(sourcefile.Info.FullName) ?? "./", "QuickConverter");
                        string destFolder = DestFolder;
                        if (UseSourceFolderAsDest)
                        {
                            destFolder = subFolder;
                        }
                        if (UseSourceFolderAsDest && Directory.Exists(subFolder) == false)
                        {
                            Directory.CreateDirectory(subFolder);
                        }
                        var destfileName = DestFiles.ElementAt(i).ShortFileName;
                        var destFile = Path.Combine(destFolder, destfileName);
                        var task = FFMpegArguments
                        .FromFileInput(sourcefile.Info.FullName)
                        .OutputToFile(destFile, false, options => options
                            .WithConstantRateFactor(21)
                            .WithAudioCodec(AudioCodec.LibMp3Lame)
                            .WithAudioBitrate(Bitrate)
                            .WithFastStart()).ProcessAsynchronously();
                        tasks.Add(task);
                        var awaiter = task.GetAwaiter();
                        awaiter.OnCompleted(() => UpdateProgressAndLogs(i, task, sourcefile, destFile));
                    }
                }
                await Task.WhenAll(tasks).ConfigureAwait(true);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
                Serilog.Log.Error("Erreur conversion {Exception}:", e);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateProgressAndLogs(int i, Task<bool> completedTask, FileInfoViewModel sourcefile, string destFile)
        {
            var percentage = (i + 1) / SourceFiles.Count * 100;
            Report(Tuple.Create(percentage, Path.GetFileName(sourcefile.Info.FullName)));
            UpdateLogs(completedTask, sourcefile, destFile);
        }

        private void UpdateLogs(Task<bool> completedTask, FileInfoViewModel sourcefile, string destfile)
        {
            if (completedTask is null || sourcefile is null)
            {
                return;
            }
            Logs.Add(new LogEntry() { FinalStatus = completedTask.Status, SourceFile = sourcefile.Info.FullName, DestFile = destfile });
        }

        public RelayCommand PickDestFolder { get; internal set; }

        private void PickDestFolderMethod()
        {
            var fbd = new FolderBrowserDialog
            {
                ShowNewFolderButton = true,
                UseDescriptionForTitle = true,
                Description = "Dossier de destination...",
                RootFolder = Environment.SpecialFolder.MyMusic
            };
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                DestFolder = fbd.SelectedPath;
            }
        }

        public MainViewModel()
        {
            if (SimpleIoc.Default.IsRegistered<StartupEventArgs>())
            {
                foreach (var fileOrFolder in SimpleIoc.Default.GetInstance<StartupEventArgs>().Args)
                {
                    if (Directory.Exists(fileOrFolder))
                    {
                        foreach (var entry in Directory.EnumerateFiles(fileOrFolder))
                        {
                            AddFileToSourceAndDest(entry);
                        }
                    }
                    else if (File.Exists(fileOrFolder))
                    {
                        AddFileToSourceAndDest(fileOrFolder);
                    }
                }
            }
            FFMpegOptions.Configure(new FFMpegOptions { RootDirectory = Path.Combine(Assembly.GetExecutingAssembly().Location, "ffmpeg"), TempDirectory = Path.GetTempPath() });
            PickDestFolder = new RelayCommand(PickDestFolderMethod);
            PickSourceFiles = new RelayCommand(PickSourceFilesMethod);
            Convert = new AsyncCommand(ConvertMethodAsync);
            DeleteAllSourceFiles = new RelayCommand(DeleteAllSourceFilesMethod);
        }

        private void DeleteAllSourceFilesMethod()
        {
            SourceFiles.Clear();
            DestFiles.Clear();
        }

        private void PickSourceFilesMethod()
        {
            var ofd = new OpenFileDialog()
            {
                Title = "Ajouter des fichiers source...",
                CheckFileExists = true,
                DereferenceLinks = true,
                InitialDirectory = SourceFiles.Any() ? Path.GetDirectoryName(SourceFiles.Last().Info.FullName) : Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                if (ofd.FileNames.Length > 0)
                {
                    foreach (var file in ofd.FileNames)
                    {
                        if (string.IsNullOrWhiteSpace(file) == false && File.Exists(file))
                        {
                            AddFileToSourceAndDest(file);
                        }
                    }
                }
                else if (string.IsNullOrWhiteSpace(ofd.FileName) == false && File.Exists(ofd.FileName))
                {
                    AddFileToSourceAndDest(ofd.FileName);
                }
            }
        }

        private void AddFileToSourceAndDest(string file)
        {
            var fileInfoVm = new FileInfoViewModel(file);
            fileInfoVm.ShortFileName = Path.GetFileName(fileInfoVm.Info.FullName);
            if (fileInfoVm.ShortFileName.Length > ShortFileNameCharLimit)
            {
                var trimedName = fileInfoVm.ShortFileName.Trim().Replace(" ", "", StringComparison.InvariantCultureIgnoreCase);
                fileInfoVm.ShortFileName = $"{Path.GetFileNameWithoutExtension(trimedName).Substring(0, ShortFileNameCharLimit + Path.GetExtension(trimedName).Length)}{Path.GetExtension(trimedName)}";
            }
            SourceFiles.Add(fileInfoVm);
            DestFiles.Add(fileInfoVm);
        }

        public void RemoveSourceFile(FileInfoViewModel fileInfoVm)
        {
            if (SourceFiles.Contains(fileInfoVm))
            {
                DestFiles.RemoveAt(SourceFiles.IndexOf(fileInfoVm));
                SourceFiles.Remove(fileInfoVm);
            }
        }

        private int _percentage;

        public int Percentage { get => _percentage; set { Set(nameof(Percentage), ref _percentage, value); } }

        public void Report(Tuple<int, string> value)
        {
            if (value is null)
            {
                return;
            }
            Percentage = value.Item1;
            LastProcessedFile = Path.GetFileName(value.Item2);
        }
    }
}