using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace VODPlayer;

public partial class BoolToInvertedVisibilityConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, string language)
    => value is true ? Visibility.Collapsed : Visibility.Visible;

  public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public partial class StringToBrushConverter : IValueConverter
{
  public object? Convert(object value, Type targetType, object parameter, string language)
  {
    if (value is not string hex)
      return null;

    hex = hex.TrimStart('#');

    if (hex.Length != 6)
      return new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

    var a = hex.Length == 8 ? byte.Parse(hex[6..8], System.Globalization.NumberStyles.HexNumber) : (byte)255;
    var r = byte.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber);
    var g = byte.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber);
    var b = byte.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber);

    return new SolidColorBrush(Color.FromArgb(a, r, g, b));
  }

  public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
