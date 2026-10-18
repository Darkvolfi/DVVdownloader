using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DVVdownloader.Models;

namespace DVVdownloader.Converters
{
    public class TabToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int selectedTab && parameter != null)
            {
                if (int.TryParse(parameter.ToString(), out int targetTab))
                {
                    return selectedTab == targetTab ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility vis && vis == Visibility.Visible && parameter != null)
            {
                if (int.TryParse(parameter.ToString(), out int targetTab))
                {
                    return targetTab;
                }
            }
            return Binding.DoNothing;
        }
    }

    public class TabToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int selectedTab && parameter != null)
            {
                if (int.TryParse(parameter.ToString(), out int targetTab))
                {
                    return selectedTab == targetTab;
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter != null)
            {
                if (int.TryParse(parameter.ToString(), out int targetTab))
                {
                    return targetTab;
                }
            }
            return Binding.DoNothing;
        }
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }
            return false;
        }
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? Visibility.Collapsed : Visibility.Visible;
            }
            if (value is int count)
            {
                return count > 0 ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
            {
                return v != Visibility.Visible;
            }
            return Binding.DoNothing;
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? Visibility.Visible : Visibility.Collapsed;
            }
            if (value is int count)
            {
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
            {
                return v == Visibility.Visible;
            }
            return Binding.DoNothing;
        }
    }

    public class StatusToBrushConverter : IValueConverter
    {
        private static SolidColorBrush GetBrush(string key)
        {
            return Application.Current.TryFindResource(key) as SolidColorBrush
                ?? new SolidColorBrush(Colors.Gray);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DownloadStatus status)
            {
                return status switch
                {
                    DownloadStatus.Tamamlandi => GetBrush("BrushSuccess"),
                    DownloadStatus.Hata => GetBrush("BrushError"),
                    DownloadStatus.Indiriliyor or DownloadStatus.Birlestiriliyor => GetBrush("BrushPrimaryAccent"),
                    DownloadStatus.IptalEdildi => GetBrush("BrushWarning"),
                    _ => GetBrush("BrushTextMuted")
                };
            }
            return GetBrush("BrushTextMuted");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
