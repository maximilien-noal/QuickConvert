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
        public FileInfo Info { get; }

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
            Info = new FileInfo(fullFilePath);
            Name = fullFilePath;
            RemoveSourceFile = new RelayCommand(() => SimpleIoc.Default.GetInstance<MainViewModel>().RemoveSourceFile(this));
        }
    }

    public class MainViewModel : ViewModelBase, IProgress<Tuple<int, string>>
    {
        private FileInfoViewModel? _selectedSourceFile;

        public FileInfoViewModel? SelectedSourceFile { get => _selectedSourceFile; set { Set(nameof(SelectedSourceFile), ref _selectedSourceFile, value); } }

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

        public RelayCommand OpenDestFolder { get; internal set; }

        public AsyncCommand Convert { get; internal set; }

        public RelayCommand PickSourceFiles { get; internal set; }

        public RelayCommand DeleteAllSourceFiles { get; internal set; }

        private async Task ConvertMethodAsync()
        {
            IsBusy = true;
            Percentage = 0;
            LastProcessedFile = "";
            if (SourceFiles.Any(x => File.Exists(x.Info.FullName)) == false)
            {
                System.Windows.MessageBox.Show("Aucun fichier à convertir. Ils n'existent plus ou sont vides.", "Pas de fichier en entrée", MessageBoxButton.OK, MessageBoxImage.Error);
                IsBusy = false;
                return;
            }
            try
            {
                var tasks = new List<Task<bool>>();
                for (int i = 0; i < SourceFiles.Count; i++)
                {
                    FileInfoViewModel? sourcefile = SourceFiles[i];
                    if (File.Exists(sourcefile.Info.FullName))
                    {
                        string subFolder = GetSubFolder(sourcefile);
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
                        if (UseSourceFolderAsDest && File.Exists(destFile) && sourcefile.Info.FullName != destFile)
                        {
                            File.Delete(destFile);
                        }
                        var task = new Task<bool>(() =>
                        {
                            try
                            {
                                while (File.Exists(destFile))
                                {
                                    var fileAlone = Path.GetFileNameWithoutExtension(destFile);
                                    var extension = Path.GetExtension(destFile);
                                    var date = DateTime.Now.ToLongTimeString().Replace(" ", "", StringComparison.InvariantCultureIgnoreCase);
                                    Path.GetInvalidFileNameChars().ToList().ForEach(x => date = date.Replace($"{x}", "", StringComparison.InvariantCultureIgnoreCase));
                                    destFile = Path.Combine(destFolder, $"{fileAlone}_{date}_{extension}");
                                }
                                return FFMpegArguments
                                    .FromFileInput(sourcefile.Info.FullName)
                                    .OutputToFile(destFile, false, options => options
                                        .WithAudioCodec(AudioCodec.LibMp3Lame)
                                        .WithAudioBitrate(Bitrate)
                                        .WithFastStart())
                                    .ProcessSynchronously();
                            }
                            catch (Exception e)
                            {
                                Serilog.Log.Error("Erreur conversion {Exception}:", e);
                                Serilog.Log.Error("Erreur conversion {BaseException}:", e.GetBaseException());
                                return false;
                            }
                        });
                        tasks.Add(task);
                        var awaiter = task.GetAwaiter();
                        awaiter.OnCompleted(() => UpdateProgressAndLogs(i, task, sourcefile, destFile));
                    }
                }
                tasks.ForEach(x => x.Start());
                await Task.WhenAll(tasks).ConfigureAwait(true);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.GetBaseException().Message, e.GetBaseException().GetType().ToString());
                Serilog.Log.Error("Erreur conversion {Exception}:", e);
                Serilog.Log.Error("Erreur conversion {BaseException}:", e.GetBaseException());
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static string GetSubFolder(FileInfoViewModel sourcefile)
        {
            return Path.Combine(Path.GetDirectoryName(sourcefile.Info.FullName) ?? "./", "QuickConverterJob");
        }

        private void UpdateProgressAndLogs(int i, Task<bool> completedTask, FileInfoViewModel sourcefile, string destFile)
        {
            var percentage = i / SourceFiles.Count * 100;
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

            string ffmpegFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./", "ffmpeg");
            FFMpegOptions.Configure(new FFMpegOptions { RootDirectory = ffmpegFolder, TempDirectory = Path.GetTempPath() });
            PickDestFolder = new RelayCommand(PickDestFolderMethod);
            PickSourceFiles = new RelayCommand(PickSourceFilesMethod);
            Convert = new AsyncCommand(ConvertMethodAsync);
            DeleteAllSourceFiles = new RelayCommand(DeleteAllSourceFilesMethod);
            OpenDestFolder = new RelayCommand(OpenDestFolderExecute);
        }

        private void OpenDestFolderExecute()
        {
            var sourceFile = SelectedSourceFile is null ? DestFiles.FirstOrDefault() : SelectedSourceFile;
            if (string.IsNullOrWhiteSpace(DestFolder) && !UseSourceFolderAsDest)
            {
                System.Windows.MessageBox.Show("Dossier non renseigné ou introuvable.");
            }
            else if (sourceFile != null)
            {
                var destFolder = GetSubFolder(sourceFile);
                Directory.CreateDirectory(destFolder);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = Path.GetDirectoryName(destFolder) ?? destFolder, UseShellExecute = true });
            }
            else if (Directory.Exists(DestFolder))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = DestFolder, UseShellExecute = true });
            }
            else
            {
                System.Windows.MessageBox.Show("Dossier non renseigné ou introuvable.");
            }
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
                Multiselect = true,
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
                var extension = Path.GetExtension(fileInfoVm.ShortFileName);
                var trimedName = Path.GetFileNameWithoutExtension(fileInfoVm.ShortFileName).Trim().Replace(" ", "", StringComparison.InvariantCultureIgnoreCase);
                fileInfoVm.ShortFileName = $"{trimedName.Substring(0, ShortFileNameCharLimit > trimedName.Length ? trimedName.Length : ShortFileNameCharLimit)}{extension}";
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