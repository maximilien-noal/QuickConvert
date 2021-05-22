namespace QuickConvert.Converters
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using System.Windows.Data;

    [ValueConversion(typeof(TaskStatus), typeof(string))]
    public sealed class TaskStatusToFrenchConverter : IValueConverter
    {
        /// <summary> Gets the default instance </summary>
        public static readonly TaskStatusToFrenchConverter Default = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskStatus ts && ts == TaskStatus.RanToCompletion)
            {
                return "Oui !";
            }
            return "Non";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && str == "Oui !")
            {
                return TaskStatus.RanToCompletion;
            }
            return TaskStatus.Faulted;
        }
    }
}