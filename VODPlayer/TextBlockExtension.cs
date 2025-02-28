using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Linq;

namespace VODPlayer;

// From: https://stackoverflow.com/a/40701754
public class TextBlockExtension
{
  public static readonly DependencyProperty RemoveEmptyRunsProperty =
      DependencyProperty.RegisterAttached("RemoveEmptyRuns", typeof(bool), typeof(TextBlock), new PropertyMetadata(false));

  public static readonly DependencyProperty PreserveSpaceProperty =
      DependencyProperty.RegisterAttached("PreserveSpace", typeof(bool), typeof(Run), new PropertyMetadata(false));

  public static bool GetRemoveEmptyRuns(DependencyObject obj)
    => (bool)obj.GetValue(RemoveEmptyRunsProperty);

  public static bool GetPreserveSpace(DependencyObject obj)
    => (bool)obj.GetValue(PreserveSpaceProperty);

  public static void SetPreserveSpace(DependencyObject obj, bool value)
    => obj.SetValue(PreserveSpaceProperty, value);

  public static void SetRemoveEmptyRuns(DependencyObject obj, bool value)
  {
    obj.SetValue(RemoveEmptyRunsProperty, value);

    if (value)
    {
      if (obj is TextBlock tb)
        tb.Loaded += Tb_Loaded;
      else
        throw new NotSupportedException();
    }
  }

  private static void Tb_Loaded(object sender, RoutedEventArgs e)
  {
    if (sender is not TextBlock tb)
      return;

    tb.Loaded -= Tb_Loaded;

    tb.Inlines.Where(a => a is Run run
         && string.IsNullOrWhiteSpace(run.Text)
         && !GetPreserveSpace(a))
      .ToList()
      .ForEach(s => tb.Inlines.Remove(s));
  }
}