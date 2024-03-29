﻿namespace QuickConvert;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

using AsyncAwaitBestPractices.MVVM;

using FFMpegCore;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

using Microsoft.Win32;

using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

public class MainViewModel : ViewModelBase, IProgress<Tuple<double, string>>
{
    private readonly string _appSettingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"{nameof(QuickConvert)}\\{nameof(QuickConvert)}.json");
    private const string ConversionSubFolderName = "QuickConverterJob";
    private FileInfoViewModel? _selectedSourceFile;

    public FileInfoViewModel? SelectedSourceFile { get => _selectedSourceFile; set { Set(nameof(SelectedSourceFile), ref _selectedSourceFile, value); } }

    private ObservableCollection<LogEntry> _logs = [];

    public ObservableCollection<LogEntry> Logs
    {
        get
        {
            return _logs;
        }
        internal set
        {
            Set(nameof(Logs), ref _logs, value);
        }
    }

    private int _shortFileNameCharLimit = 15;

    public int ShortFileNameCharLimit { get => _shortFileNameCharLimit; set { Set(nameof(ShortFileNameCharLimit), ref _shortFileNameCharLimit, value); } }

    private bool _useSourceFolderAsDest = true;

    public bool UseSourceFolderAsDest { get => _useSourceFolderAsDest; set { Set(nameof(UseSourceFolderAsDest), ref _useSourceFolderAsDest, value); } }

    private string _format = "MP3";

    public string Format { get => _format; set { Set(nameof(Format), ref _format, value); } }

    private ObservableCollection<string> _formats = new(new string[] { "MP3" });

    public ObservableCollection<string> Formats { get => _formats; internal set { Set(nameof(Formats), ref _formats, value); } }

    private int _bitrate = 224;

    public int Bitrate { get => _bitrate; set { Set(nameof(Bitrate), ref _bitrate, value); } }

    private ObservableCollection<int> _bitrates = new(new int[] { 192, 224, 256, 320 });

    public ObservableCollection<int> Bitrates { get => _bitrates; internal set { Set(nameof(Bitrates), ref _bitrates, value); } }

    private bool _isBusy = false;
    private Prefs? _prefs;

    public bool IsBusy { get => _isBusy; set { Set(nameof(IsBusy), ref _isBusy, value); } }

    private string _lastProcessedFile = "";

    public string LastProcessedFile { get => _lastProcessedFile; set { Set(nameof(LastProcessedFile), ref _lastProcessedFile, value); } }

    private ObservableCollection<FileInfoViewModel> _sourceFiles = [];

    public ObservableCollection<FileInfoViewModel> SourceFiles { get => _sourceFiles; internal set { Set(nameof(SourceFiles), ref _sourceFiles, value); } }

    private ObservableCollection<FileInfoViewModel> _destFiles = [];

    public ObservableCollection<FileInfoViewModel> DestFiles { get => _destFiles; internal set { Set(nameof(DestFiles), ref _destFiles, value); } }

    private string _destFolder = "";

    public string DestFolder { get => _destFolder; set { Set(nameof(DestFolder), ref _destFolder, value); } }

    public RelayCommand Close { get; internal set; } = new RelayCommand(() => Application.Current.MainWindow.Close());

    public RelayCommand OpenDestFolder { get; internal set; }

    public AsyncCommand Convert { get; internal set; }

    public RelayCommand PickSourceFolder { get; internal set; }

    public RelayCommand PickSourceFiles { get; internal set; }

    public RelayCommand DeleteAllSourceFiles { get; internal set; }

    public RelayCommand CancelEntireConversion {  get; internal set; }

    private void CancelConversion()
    {
        if(MessageBox.Show("Voulez-vous vraiment annuler la conversion en cours ?", "Annuler la conversion", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            IsBusy = false;
        }
    }

    private string _textProgress = "Prêt";

    public string TextProgress { get => _textProgress; set { Set(nameof(TextProgress), ref _textProgress, value); } }


    private readonly string[] _extensions = ["*.mov", "*.webm","*.m4a", ".flac", ".mp3", ".ape", ".mpc", ".ogg", ".wav", ".mp4", ".mkv", ".vob", ".aac", ".ac3", ".wav", ".wma", ".avi", ".ogv", ".tta", ".mpg", ".mpeg"];

    private async Task ConvertMethodAsync()
    {
        Report(Tuple.Create(0d, ""));
        if (SourceFiles.All(x => File.Exists(x.Info.FullName)) == false)
        {
            MessageBox.Show("Aucun fichier à convertir. Ils n'existent plus ou sont inaccessibles.", "Pas de fichier en entrée", MessageBoxButton.OK, MessageBoxImage.Error);
            IsBusy = false;
            return;
        }
        IsBusy = true;
        TextProgress = "Conversion en cours...";
        try
        {
            for (int i = 0; i < SourceFiles.Where(x => File.Exists(x.Info.FullName)).Count(); i++)
            {
                // Check if cancellation is requested
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    break; // Break the loop and stop the conversion process
                }

                FileInfoViewModel? sourcefile = SourceFiles[i];
                var subFolder = sourcefile.GetSubFolder();
                if (UseSourceFolderAsDest && Directory.Exists(subFolder) == false)
                {
                    Directory.CreateDirectory(subFolder);
                }
                var destFile = sourcefile.GetDestFile(i);
                if (UseSourceFolderAsDest && File.Exists(destFile) && sourcefile.Info.FullName != destFile)
                {
                    File.Delete(destFile);
                }
                destFile = sourcefile.GetDestFileNameForConversion(i);
                if (destFile.ToUpperInvariant().EndsWith(".MP3") == false)
                {
                    destFile = $"{destFile}.mp3";
                }
                var ffmpegProc = FFMpegArguments
                    .FromFileInput(sourcefile.Info.FullName)
                    .OutputToFile(destFile, false, options => options
                        .OverwriteExisting()
                        .WithCustomArgument($"-c:a libmp3lame -b:a {Bitrate}k")
                        .WithFastStart());
                var task = Task.Run(() => ffmpegProc.ProcessAsynchronously(), _cancellationTokenSource.Token);

                var awaiter = task.GetAwaiter();
                awaiter.OnCompleted(() =>
                {
                    UpdateProgress(i, sourcefile);
                    UpdateLogs(task, sourcefile, destFile);
                });
                try
                {
                    await task.ConfigureAwait(true);
                }
                catch (Exception e)
                {
                    Serilog.Log.Error("Erreur conversion {Exception}:", e);
                    Serilog.Log.Error("Erreur conversion {BaseException}:", e.GetBaseException());
                    UpdateLogs(task, sourcefile, destFile);
                }
            }
        }
        catch (Exception e)
        {
            MessageBox.Show(e.GetBaseException().Message, e.GetBaseException().GetType().ToString());
            Serilog.Log.Error("Erreur conversion {Exception}:", e);
            Serilog.Log.Error("Erreur conversion {BaseException}:", e.GetBaseException());
        }
        finally
        {
            IsBusy = false;
            TaskbarProgress.SetState(new WindowInteropHelper(Application.Current.MainWindow).Handle, TaskbarProgress.TaskbarStates.Indeterminate);
            TextProgress = "Conversion terminée";
        }
    }

    private void UpdateProgress(int i, FileInfoViewModel sourcefile)
    {
        var percentage = (double)i / SourceFiles.Count * 100;
        Report(Tuple.Create(percentage, Path.GetFileName(sourcefile.Info.FullName)));
        TextProgress = $"Conversion en cours... {percentage:0.00}%";
    }

    private void UpdateLogs(Task<bool> completedTask, FileInfoViewModel sourcefile, string destfile)
    {
        if (completedTask is null || sourcefile is null)
        {
            return;
        }
        Logs.Add(new LogEntry() { FinalStatus = completedTask.Status, SourceFile = sourcefile.Info.FullName, DestFile = destfile, Error = completedTask.Exception });
    }

    public RelayCommand PickDestFolder { get; internal set; }

    private void PickDestFolderMethod()
    {
        var fbd = new OpenFolderDialog()
        {
            ShowHiddenItems = false,
            Multiselect = false,
            Title = "Dossier de destination...",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
        };
        if (fbd.ShowDialog() == true)
        {
            DestFolder = fbd.FolderName;
        }
    }

    private static List<string> EnumerateFilesRecursively(string folderPath)
    {
        if ((Path.GetFileName(folderPath)).Equals(ConversionSubFolderName, StringComparison.InvariantCultureIgnoreCase))
        {
            return [];
        }
        var files = new List<string>(Directory.EnumerateFiles(folderPath));
        foreach (var dir in Directory.EnumerateDirectories(folderPath))
        {
            files.AddRange(EnumerateFilesRecursively(dir));
        }
        return files;
    }

    public MainViewModel()
    {
        string ffmpegFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./", "ffmpeg");
        FFMpegCore.GlobalFFOptions.Configure(new FFOptions() { BinaryFolder = ffmpegFolder, TemporaryFilesFolder = Path.GetTempPath() });
        CancelEntireConversion = new RelayCommand(CancelConversion);
        PickDestFolder = new RelayCommand(PickDestFolderMethod);
        PickSourceFiles = new RelayCommand(PickSourceFilesMethod);
        PickSourceFolder = new RelayCommand(PickSourceFolderMethod);
        Convert = new AsyncCommand(ConvertMethodAsync);
        DeleteAllSourceFiles = new RelayCommand(DeleteAllSourceFilesMethod);
        OpenDestFolder = new RelayCommand(OpenDestFolderMethod);
    }

    public async Task LoadAppSettingsAsync()
    {
        IsBusy = true;
        if (File.Exists(_appSettingsFilePath))
        {
            _prefs = await new ModelSerializer<Prefs>().DeserializeAsync<Prefs>(_appSettingsFilePath).ConfigureAwait(true);
        }
        else
        {
            _prefs = new Prefs();
        }
        UseSourceFolderAsDest = _prefs.UseSourceFolderAsDest;
        ShortFileNameCharLimit = _prefs.ShortFileNameCharLimit;
        IsBusy = false;
    }

    public async Task SaveAppSettingsAsync()
    {
        IsBusy = true;
        var configDir = Path.GetDirectoryName(_appSettingsFilePath);
        if (configDir != null)
        {
            Directory.CreateDirectory(configDir);
        }
        if (_prefs is null)
        {
            _prefs = new Prefs();
        }
        _prefs.UseSourceFolderAsDest = UseSourceFolderAsDest;
        _prefs.ShortFileNameCharLimit = ShortFileNameCharLimit;
        await new ModelSerializer<Prefs>().SerializeAsync(_appSettingsFilePath, _prefs).ConfigureAwait(true);
        IsBusy = false;
    }

    private void PickSourceFolderMethod()
    {
        var fbd = new OpenFolderDialog()
        {
            Title = "Ajouter des fichiers source...",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
        };
        if (fbd.ShowDialog() == true)
        {
            if (string.IsNullOrWhiteSpace(fbd.FolderName) == false && Directory.Exists(fbd.FolderName))
            {
                foreach (var file in EnumerateFilesRecursively(fbd.FolderName))
                {
                    AddFileToSourceAndDestIfExtensionIsValid(file);
                }
            }
        }
    }

    private void AddFileToSourceAndDestIfExtensionIsValid(string file)
    {
        if (_extensions.Contains(Path.GetExtension(file)))
        {
            AddFileToSourceAndDest(file);
        }
    }

    private void OpenDestFolderMethod()
    {
        var sourceFile = SelectedSourceFile is null ? DestFiles.FirstOrDefault() : SelectedSourceFile;
        if (string.IsNullOrWhiteSpace(DestFolder) && !UseSourceFolderAsDest)
        {
            MessageBox.Show($"Dossier non renseigné ou introuvable :{Environment.NewLine}{DestFolder}");
        }
        else if (sourceFile != null)
        {
            var destFolder = sourceFile.GetSubFolder();
            Directory.CreateDirectory(destFolder);
            Process.Start(new ProcessStartInfo { FileName = Path.GetDirectoryName(destFolder) ?? destFolder, UseShellExecute = true });
        }
        else if (Directory.Exists(DestFolder))
        {
            Process.Start(new ProcessStartInfo { FileName = DestFolder, UseShellExecute = true });
        }
        else
        {
            MessageBox.Show($"Dossier non renseigné ou introuvable :{Environment.NewLine}{DestFolder}");
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
            Filter = "Fichier audio ou vidéo (*.mov;*.webm;*.m4a;*.flac;*.mp3;*.ape;*.mpc;*.ogg;*.wav;*.mp4;*.mkv;*.vob;*.aac;*.ac3;*.wav;*.wma;*.avi;*.ogv;*.tta;*.mpg;*.mpeg)|*.mov;*.webm;*.m4a;*.flac;*.mp3;*.ape;*.mpc;*.ogg;*.wav;*.mp4;*.mkv;*.vob;*.aac;*.ac3;*.wav;*.wma;*.avi;*.ogv;*.tta;*.mpg;*.mpeg|Tous les types de fichiers (*.*)|*.*",
            InitialDirectory = SourceFiles.Any() ? Path.GetDirectoryName(SourceFiles.Last().Info.FullName) : Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
        };
        if (ofd.ShowDialog() == true)
        {
            AddPickedFiles(ofd);
        }
    }

    private void AddPickedFiles(OpenFileDialog ofd)
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

    private void AddFileToSourceAndDest(string file)
    {
        var fileInfoVm = new FileInfoViewModel(this, file);
        fileInfoVm.ShortFileName = Path.GetFileName(fileInfoVm.Info.FullName);
        if (fileInfoVm.ShortFileName.Length > ShortFileNameCharLimit)
        {
            fileInfoVm.ShortFileName = GetShortFileName(fileInfoVm);
        }
        SourceFiles.Add(fileInfoVm);
        DestFiles.Add(fileInfoVm);
    }

    private string GetShortFileName(FileInfoViewModel fileInfoVm)
    {
        var extension = ".mp3";
        string trimedName = GetTrimedName(fileInfoVm);
        int offset = DestFiles.Count(x => Path.GetFileNameWithoutExtension(x.ShortFileName).Equals(Path.GetFileNameWithoutExtension(trimedName), StringComparison.InvariantCultureIgnoreCase));
        while (DestFiles.Any(x => Path.GetFileNameWithoutExtension(x.ShortFileName).Equals(Path.GetFileNameWithoutExtension(trimedName), StringComparison.InvariantCultureIgnoreCase)))
        {
            trimedName = GetTrimedName(fileInfoVm, offset++);
        }
        return $"{trimedName}{extension}";
    }

    private string GetTrimedName(FileInfoViewModel fileInfoVm, int offset = 0)
    {
        if (offset == 0)
        {
            var trimedName = GetNameWithoutSpaces(fileInfoVm);
            return $"{trimedName[..(ShortFileNameCharLimit > trimedName.Length ? trimedName.Length : ShortFileNameCharLimit)]}";
        }
        else
        {
            var trimedName = GetNameWithoutSpaces(fileInfoVm);
            return $"{trimedName[..(ShortFileNameCharLimit > trimedName.Length ? trimedName.Length : ShortFileNameCharLimit)]}_{offset}";
        }
    }

    private static string GetNameWithoutSpaces(FileInfoViewModel fileInfoVm) => Path.GetFileNameWithoutExtension(fileInfoVm.ShortFileName).Trim().Replace(" ", "", StringComparison.InvariantCultureIgnoreCase);

    public void RemoveSourceFile(FileInfoViewModel fileInfoVm)
    {
        if (SourceFiles.Contains(fileInfoVm))
        {
            DestFiles.RemoveAt(SourceFiles.IndexOf(fileInfoVm));
            SourceFiles.Remove(fileInfoVm);
        }
    }

    private double _percentage;

    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public double Percentage { get => _percentage; set { Set(nameof(Percentage), ref _percentage, value); } }

    public void Report(Tuple<double, string> value)
    {
        if (value is null)
        {
            return;
        }
        Percentage = value.Item1;
        LastProcessedFile = Path.GetFileName(value.Item2);
        if (Percentage == 0)
        {
            TaskbarProgress.SetState(new WindowInteropHelper(Application.Current.MainWindow).Handle, TaskbarProgress.TaskbarStates.Indeterminate);
        }
        else
        {
            TaskbarProgress.SetValue(new WindowInteropHelper(Application.Current.MainWindow).Handle, Percentage, 100);
        }
    }

    internal void AddStartupDestFileOrFolder(string[] processArguments)
    {
        foreach (var fileOrFolder in processArguments)
        {
            if (Directory.Exists(fileOrFolder))
            {
                foreach (var file in EnumerateFilesRecursively(fileOrFolder))
                {
                    AddFileToSourceAndDestIfExtensionIsValid(file);
                }
            }
            else if (File.Exists(fileOrFolder))
            {
                AddFileToSourceAndDestIfExtensionIsValid(fileOrFolder);
            }
        }
        
    }
}