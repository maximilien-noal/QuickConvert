namespace QuickConvert.Converters;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BooleanToInvertVisibilityConverter : IValueConverter
{
    public static readonly BooleanToInvertVisibilityConverter Default = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolean)
        {
            if (boolean == false)
            {
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility vsb)
        {
            if (vsb == Visibility.Collapsed)
            {
                return false;
            }
            return true;
        }
        return true;
    }
}