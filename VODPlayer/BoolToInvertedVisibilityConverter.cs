using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace VODPlayer;

public partial class BoolToInvertedVisibilityConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, string language)
    => value is true ? Visibility.Collapsed : Visibility.Visible;

  public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
