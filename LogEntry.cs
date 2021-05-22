namespace QuickConvert
{
    using GalaSoft.MvvmLight;

    using System;
    using System.Threading.Tasks;

    public class LogEntry : ObservableObject
    {
        public DateTimeOffset Date => DateTimeOffset.Now;

        private TaskStatus _finalStatus;

        public TaskStatus FinalStatus { get => _finalStatus; internal set { Set(nameof(FinalStatus), ref _finalStatus, value); } }

        private string _sourceFile = "";

        public string SourceFile { get => _sourceFile; internal set { Set(nameof(SourceFile), ref _sourceFile, value); } }

        private string _destFile = "";

        public string DestFile { get => _destFile; internal set { Set(nameof(DestFile), ref _destFile, value); } }

        private AggregateException? _error = null;

        public AggregateException? Error { get => _error; internal set { Set(nameof(Error), ref _error, value); } }
    }
}