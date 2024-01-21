namespace QuickConvert;


using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

public class FileInfoViewModel : ViewModelBase
{
    public FileInfo Info { get; }

    private const string ConversionSubFolderName = "QuickConverterJob";
    private string _fullPath = "";

    public string FullPath { get => _fullPath; set { Set(nameof(FullPath), ref _fullPath, value); } }

    private string _name = "";

    public string Name { get => _name; set { Set(nameof(Name), ref _name, value); } }

    private string _shortFileName = "";

    public string ShortFileName { get => _shortFileName; set { Set(nameof(ShortFileName), ref _shortFileName, value); } }

    public RelayCommand RemoveSourceFile { get; internal set; }
    public RelayCommand PlaySourceFile { get; internal set; }

    public string GetSubFolder()
    {
        return Path.Combine(Path.GetDirectoryName(Info.FullName) ?? "./", ConversionSubFolderName);
    }

    public string DestFile
    {
        get
        {
            return GetDestFile(_mainWindowViewModel.DestFiles.IndexOf(this));
        }
    }

    public string GetDestFile(int i)
    {
        string destFolder = GetDestFolder();
        var destfileName = _mainWindowViewModel.DestFiles.ElementAtOrDefault(i)?.ShortFileName;
        if(string.IsNullOrWhiteSpace(destfileName))
        {
            destfileName = Path.GetFileNameWithoutExtension(Info.FullName) + ".MP3";
        }
        var destFile = Path.Combine(destFolder, destfileName);
        return destFile;
    }

    private string GetDestFolder()
    {
        string destFolder = _mainWindowViewModel.DestFolder;
        if (_mainWindowViewModel.UseSourceFolderAsDest)
        {
            destFolder = GetSubFolder();
        }

        return destFolder;
    }

    public string GetDestFileNameForConversion(int i)
    {
        var destFile = GetDestFile(i);
        while (File.Exists(destFile))
        {
            var fileAlone = Path.GetFileNameWithoutExtension(destFile);
            var extension = ".MP3";
            var date = DateTime.Now.ToLongTimeString().Replace(" ", "", StringComparison.InvariantCultureIgnoreCase);
            Path.GetInvalidFileNameChars().ToList().ForEach(x => date = date.Replace($"{x}", "", StringComparison.InvariantCultureIgnoreCase));
            destFile = Path.Combine(GetDestFolder(), $"{fileAlone}_{date}_{extension}");
        }

        return destFile;
    }

    private readonly MainViewModel _mainWindowViewModel;

    public FileInfoViewModel(MainViewModel mainViewModel, string fullFilePath)
    {
        _mainWindowViewModel = mainViewModel;
        if (string.IsNullOrWhiteSpace(fullFilePath) || File.Exists(fullFilePath) == false)
        {
            throw new FileNotFoundException(fullFilePath);
        }
        Info = new FileInfo(fullFilePath);
        Name = Path.GetFileName(fullFilePath);
        FullPath = fullFilePath;

        RemoveSourceFile = new RelayCommand(() => _mainWindowViewModel.RemoveSourceFile(this));
        PlaySourceFile = new RelayCommand(() => Process.Start(new ProcessStartInfo(Info.FullName) { UseShellExecute = true }));
    }
}