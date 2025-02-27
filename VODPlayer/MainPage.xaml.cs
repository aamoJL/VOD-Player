using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Core;
using Windows.Storage;

namespace VODPlayer;

public sealed partial class MainPage : Page, INotifyPropertyChanged
{
  private const string _mp4ContentType = "video/mp4";

  public bool IsPaneOpen
  {
    get;
    set => SetProperty(ref field, value);
  } = true;

  public event PropertyChangedEventHandler? PropertyChanged;

  public Action? FullscreenAction { get; set; }

  private bool _singleTap = true; // Prevents single and double tapping event conflict

  public MainPage()
  {
    InitializeComponent();

    AllowDrop = true;

    DragEnter += OnDragEnter;
    Drop += OnDrop;
  }

  protected override void OnNavigatedTo(NavigationEventArgs e)
  {
    base.OnNavigatedTo(e);

    if (e.Parameter is string filePath && File.Exists(filePath) && Path.GetExtension(filePath) is ".mp4")
      MediaPlayer.Source = MediaSource.CreateFromUri(new Uri(filePath));
  }

  [NotNull]
  public ICommand? PanelOpenCommand => field ??= new RelayCommand(execute: () => { IsPaneOpen = !IsPaneOpen; });

  private async void OnDragEnter(object sender, Microsoft.UI.Xaml.DragEventArgs e)
  {
    var def = e.GetDeferral();

    if (e.DataView.Contains(StandardDataFormats.StorageItems) && (await e.DataView.GetStorageItemsAsync()).Any(x => x is StorageFile { ContentType: _mp4ContentType }))
    {
      // Accept .mp4 files
      e.AcceptedOperation = ~DataPackageOperation.None;
      e.DragUIOverride.Caption = "Open";
    }

    e.Handled = true;

    def.Complete();
  }

  private async void OnDrop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
  {
    var def = e.GetDeferral();

    if ((await e.DataView.GetStorageItemsAsync()).FirstOrDefault(x => x is StorageFile { ContentType: _mp4ContentType }) is StorageFile video)
      MediaPlayer.Source = MediaSource.CreateFromUri(new Uri(video.Path));

    e.Handled = true;

    def.Complete();
  }

  private void SetProperty<T>(ref T field, T value)
  {
    if (field == null)
      return;

    if (!field.Equals(value))
    {
      field = value;
      PropertyChanged?.Invoke(this, new(nameof(IsPaneOpen)));
    }
  }

  private async void MediaPlayer_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
  {
    _singleTap = true;
    await Task.Delay(200);

    if (_singleTap)
    {
      if (MediaPlayer.MediaPlayer.CurrentState == Windows.Media.Playback.MediaPlayerState.Playing)
        MediaPlayer.MediaPlayer.Pause();
      else if (MediaPlayer.MediaPlayer.CurrentState == Windows.Media.Playback.MediaPlayerState.Paused)
        MediaPlayer.MediaPlayer.Play();
    }
  }

  private void MediaPlayer_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
  {
    _singleTap = false;

    FullscreenAction?.Invoke();

    e.Handled = true;
  }

  private void MediaPlayerStateKeyboardAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
  {
    if (MediaPlayer.MediaPlayer.CurrentState == Windows.Media.Playback.MediaPlayerState.Playing)
      MediaPlayer.MediaPlayer.Pause();
    else if (MediaPlayer.MediaPlayer.CurrentState == Windows.Media.Playback.MediaPlayerState.Paused)
      MediaPlayer.MediaPlayer.Play();
  }
}