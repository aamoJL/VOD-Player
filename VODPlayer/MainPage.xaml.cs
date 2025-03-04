using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
  private int currentIndex = 0;
  private bool _bringToView = true;

  public MainPage()
  {
    InitializeComponent();

    AllowDrop = true;

    DragEnter += OnDragEnter;
    Drop += OnDrop;

    var timer = new DispatcherTimer() { Interval = new(0, 0, 2) };

    timer.Tick += Timer_Tick;
    timer.Start();
  }

  [NotNull] public ICommand? PanelOpenCommand => field ??= new RelayCommand(execute: () => { IsPaneOpen = !IsPaneOpen; });

  private void ChangeVideo(StorageFile video)
  {
    try
    {
      var videoFilePath = video.Path;

      if (!File.Exists(videoFilePath))
        throw new Exception("Video file was not found");

      MediaPlayer.Source = MediaSource.CreateFromUri(new Uri(video.Path));
    }
    catch (Exception ex)
    {
      MediaPlayer.Source = null;

      Debug.WriteLine(ex.Message);
      return;
    }

    try
    {
      var chatFilePath = Mp4SuffixRegex().Replace(video.Path, ".json");

      if (!File.Exists(chatFilePath))
        throw new Exception("Comments file was not found");

      if (JsonNode.Parse(File.ReadAllText(chatFilePath)) is not JsonNode chatJson)
        throw new Exception("No comments found");

      Comments = ParseComments(chatJson);
    }
    catch (Exception ex)
    {
      Comments = [];

      Debug.WriteLine(ex.Message);
    }
    finally
    {
      currentIndex = 0;
    }
  }

  private static List<Comment> ParseComments(JsonNode json)
  {
    if (json?["comments"]?.AsArray() is not JsonArray commentsJson)
    {
      Debug.WriteLine("Json does not have comments");
      return [];
    }

    var list = new List<Comment>() { Capacity = commentsJson.Count };

    foreach (var commentJson in commentsJson)
    {
      try
      {
        var commenter = commentJson?["commenter"]?["display_name"]?.GetValue<string>();
        var color = commentJson?["message"]?["user_color"]?.GetValue<string>();
        var fragments = commentJson?["message"]?["fragments"]?.AsArray().Select(fragment =>
        {
          return fragment?["text"]?.GetValue<string>() is string text ? new Comment.Fragment() { Text = text } : null;
        }).OfType<Comment.Fragment>();
        var offsetSeconds = commentJson?["content_offset_seconds"]?.GetValue<uint>();

        list.Add(new Comment()
        {
          Commenter = commenter ?? throw new Exception("Commenter is null"),
          Fragments = fragments?.ToArray() ?? throw new Exception("Fragments is null"),
          OffsetSeconds = offsetSeconds ?? throw new Exception("Offset is null"),
          Color = color ?? "#FF0000",
        });
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex.Message);
      }
    }

    return list;
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

  private void Timer_Tick(object? sender, object e)
  {
    if (Comments.Count <= 0)
      return;

    if (currentIndex > Comments.Count)
      currentIndex = Comments.Count - 1;

    var startIndex = currentIndex;
    var positionSeconds = (uint)MediaPlayer.MediaPlayer.Position.TotalSeconds;
    var lastMessageSeconds = currentIndex != -1 ? Comments[currentIndex].OffsetSeconds : 0;

    if (positionSeconds > lastMessageSeconds)
    {
      // Forward
      for (var i = currentIndex; i < Comments.Count; i++)
      {
        if (Comments[i].OffsetSeconds <= positionSeconds)
        {
          Comments[i].IsSent = true;
          currentIndex = i;
        }
        else
          break;
      }
    }
    else
    {
      // Backwards
      for (var i = currentIndex; i >= 0; i--)
      {
        if (Comments[i].OffsetSeconds > positionSeconds)
        {
          Comments[i].IsSent = false;
          currentIndex = i;
        }
        else
          break;
      }
    }

    if (_bringToView && startIndex != currentIndex)
    {
      var lastElement = CommentsItemsRepeater.GetOrCreateElement(currentIndex);

      CommentsItemsRepeater.UpdateLayout();

      lastElement.StartBringIntoView(new()
      {
        AnimationDesired = true,
        VerticalAlignmentRatio = 1f
      });
    }
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