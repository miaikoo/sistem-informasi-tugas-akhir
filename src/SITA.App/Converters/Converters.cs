using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SITA.App.Converters;

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Visible when the value is non-null and not an empty string.</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is null || value is string { Length: 0 } ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Visible when a count is zero; used for "no data" placeholders.</summary>
public class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is not true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is not true;
}

/// <summary>Formats a timestamp as "baru saja", "5 menit lalu", "kemarin", ...</summary>
public class RelativeTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DateTime time) return "";
        var diff = DateTime.Now - time;
        if (diff.TotalMinutes < 1) return "baru saja";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} menit lalu";
        if (diff.TotalHours < 24 && time.Date == DateTime.Today) return $"{(int)diff.TotalHours} jam lalu";
        if (time.Date == DateTime.Today.AddDays(-1)) return "kemarin, " + time.ToString("HH:mm");
        return time.ToString("d MMM yyyy, HH:mm", new CultureInfo("id-ID"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
