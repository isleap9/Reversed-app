using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace VainTools.GpuGovernor.Converters;

/// <summary>
/// Converts a temperature value to a color for display.
/// Green: < 70°C, Yellow: 70-85°C, Red: > 85°C
/// </summary>
public class TemperatureToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int temp)
        {
            if (temp < 70)
                return new SolidColorBrush(Microsoft.UI.Colors.Green);
            if (temp <= 85)
                return new SolidColorBrush(Microsoft.UI.Colors.Orange);
            return new SolidColorBrush(Microsoft.UI.Colors.Red);
        }
        return new SolidColorBrush(Microsoft.UI.Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
