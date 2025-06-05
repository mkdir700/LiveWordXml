using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using LiveWordXml.Wpf.Models;

namespace LiveWordXml.Wpf
{
    /// <summary>
    /// 高亮背景转换器
    /// </summary>
    public class HighlightBackgroundConverter : IValueConverter
    {
        public static readonly HighlightBackgroundConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isHighlighted && isHighlighted)
            {
                return new SolidColorBrush(Colors.LightYellow);
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 节点计数转换器
    /// </summary>
    public class NodeCountConverter : IValueConverter
    {
        public static readonly NodeCountConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DocumentStructureNode node)
            {
                var count = CountNodesRecursive(node);
                return $"Total nodes: {count}";
            }
            return "No structure";
        }

        private int CountNodesRecursive(DocumentStructureNode node)
        {
            int count = 1; // Count current node
            foreach (var child in node.Children)
            {
                count += CountNodesRecursive(child);
            }
            return count;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值到可见性转换器（内置转换器的静态实例）
    /// </summary>
    public static class BooleanToVisibilityConverter
    {
        public static readonly System.Windows.Controls.BooleanToVisibilityConverter Default = new();
    }

    /// <summary>
    /// 反向布尔值到可见性转换器
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public static readonly InverseBooleanToVisibilityConverter Default = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            }
            return System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Visibility visibility)
            {
                return visibility == System.Windows.Visibility.Collapsed;
            }
            return false;
        }
    }
}