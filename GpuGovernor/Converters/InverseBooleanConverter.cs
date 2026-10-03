using System;
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace VainTools.GpuGovernor.Converters;

/// <summary>
/// Converts a boolean value to its inverse (true becomes false, false becomes true).
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
            return !b;
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
            return !b;
        return value;
    }
}
