using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ScreenTimeTracker.Views.Converters;

public class SecondsToTimeStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double minutes)
            return FormatMinutes(minutes);
        if (value is int intMinutes)
            return FormatMinutes(intMinutes);
        if (value is long seconds)
            return FormatMinutes(seconds / 60.0);

        return "0m";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static string FormatMinutes(double minutes)
    {
        if (minutes < 1) return "< 1m";
        var hours = (int)(minutes / 60);
        var mins = (int)(minutes % 60);
        if (hours > 0 && mins > 0) return $"{hours}h {mins}m";
        if (hours > 0) return $"{hours}h";
        return $"{mins}m";
    }
}

public class UsageToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double maxWidth = 120;
        if (parameter is double customMax)
            maxWidth = customMax;

        if (value is double ratio)
            return Math.Min(ratio * maxWidth, maxWidth);

        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;
        bool invert = parameter is string s && s == "Invert";

        if (invert) boolValue = !boolValue;
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility vis && vis == Visibility.Visible;
    }
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
}

public class RatioToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double ratio)
        {
            if (ratio >= 1.0)
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3B30"));
            if (ratio >= 0.8)
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9500"));
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007AFF"));
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007AFF"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string s && !string.IsNullOrWhiteSpace(s);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
