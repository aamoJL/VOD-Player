using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace VODPlayer;

public sealed partial class CustomMediaTransportControls : MediaTransportControls
{
  public static readonly DependencyProperty PlaybackRateProperty =
      DependencyProperty.Register(nameof(PlaybackRate), typeof(double), typeof(CustomMediaTransportControls), new PropertyMetadata(1d));

  public static readonly DependencyProperty FullScreenCommandProperty =
      DependencyProperty.Register(nameof(FullScreenCommand), typeof(ICommand), typeof(CustomMediaTransportControls), new PropertyMetadata(null));

  public CustomMediaTransportControls()
    => DefaultStyleKey = typeof(CustomMediaTransportControls);

  public double PlaybackRate
  {
    get => (double)GetValue(PlaybackRateProperty);
    set => SetValue(PlaybackRateProperty, value);
  }

  public ICommand FullScreenCommand
  {
    get => (ICommand)GetValue(FullScreenCommandProperty);
    set => SetValue(FullScreenCommandProperty, value);
  }
}
