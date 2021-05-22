namespace QuickConvert.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    [ValueConversion(typeof(AggregateException), typeof(string))]
    public sealed class AggregateExceptionToStringConverter : IValueConverter
    {
        /// <summary> Gets the default instance </summary>
        public static readonly AggregateExceptionToStringConverter Default = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AggregateException e)
            {
                var exception = e.GetBaseException();
                return $"{exception.Message}{Environment.NewLine}{exception.StackTrace}";
            }
            return "";
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}