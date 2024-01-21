namespace QuickConvert.Converters;

using System;
using System.Globalization;
using System.Windows.Data;

[ValueConversion(typeof(bool), typeof(bool))]
public sealed class BooleanToInvertBooleanConverter : IValueConverter
{
    /// <summary> Gets the default instance </summary>
    public static readonly BooleanToInvertBooleanConverter Default = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolean)
        {
            return !boolean;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolean)
        {
            return boolean;
        }
        return true;
    }
}