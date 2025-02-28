using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Core;
using Windows.Storage;

namespace VODPlayer;

public sealed partial class MainPage : Page, INotifyPropertyChanged
{
  [GeneratedRegex(".mp4$")] private static partial Regex Mp4SuffixRegex();
  private const string _mp4ContentType = "video/mp4";

  public bool IsPaneOpen
  {
    get;
    set => SetProperty(ref field, value);
  } = true;
  public List<Comment> Comments
  {
    get;
    set => SetProperty(ref field, value);
  } = [];

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

  [NotNull] public ICommand? PanelOpenCommand => field ??= new RelayCommand(execute: () => { IsPaneOpen = !IsPaneOpen; });

  private void ChangeVideo(StorageFile video)
  {
    MediaPlayer.Source = MediaSource.CreateFromUri(new Uri(video.Path));

    var chatFilePath = Mp4SuffixRegex().Replace(video.Path, ".json");

    if (!File.Exists(chatFilePath))
      return;

    var list = new List<Comment>();

    try
    {
      if (JsonNode.Parse(File.ReadAllText(chatFilePath))?["comments"]?.AsArray() is not JsonArray commentsJson)
        return;

      foreach (var commentJson in commentsJson)
      {
        var commenter = commentJson?["commenter"]?["display_name"]?.GetValue<string>();
        var fragments = commentJson?["message"]?["fragments"]?.AsArray().Select(fragment =>
        {
          return fragment?["text"]?.GetValue<string>() is string text ? new Comment.Fragment() { Text = text } : null;
        }).OfType<Comment.Fragment>();
        var color = commentJson?["message"]?["user_color"]?.GetValue<string>();

        if (string.IsNullOrEmpty(commenter) || fragments?.Any() is not true)
          return;

        var comment = new Comment()
        {
          Commenter = commenter,
          Fragments = fragments.ToArray(),
          Color = color ?? "#FF0000"
        };

        list.Add(comment);
      }
    }
    catch { }

    Comments = list;
  }

  protected override async void OnNavigatedTo(NavigationEventArgs e)
  {
    base.OnNavigatedTo(e);

    try
    {
      if (e.Parameter is string filePath && File.Exists(filePath) && Path.GetExtension(filePath) is ".mp4")
        ChangeVideo(await StorageFile.GetFileFromPathAsync(filePath));
    }
    catch { }
  }

  private async void OnDragEnter(object sender, DragEventArgs e)
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

  private async void OnDrop(object sender, DragEventArgs e)
  {
    var def = e.GetDeferral();

    if ((await e.DataView.GetStorageItemsAsync()).FirstOrDefault(x => x is StorageFile { ContentType: _mp4ContentType }) is StorageFile video)
      ChangeVideo(video);

    e.Handled = true;

    def.Complete();
  }

  private async void MediaPlayer_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
  {
    if (e.OriginalSource is not (Border or FrameworkElement { Name: "RootGrid" }))
      return; // Prevents state change when interacting with mediaplayer controls

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

  private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
  {
    if (field == null)
      return;

    if (!field.Equals(value))
    {
      field = value;
      PropertyChanged?.Invoke(this, new(propertyName));
    }
  }
}