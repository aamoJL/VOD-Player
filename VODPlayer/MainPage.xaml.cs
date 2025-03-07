using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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
  public bool StickyChat
  {
    get;
    set
    {
      if (SetProperty(ref field, value))
        StickToChatCommand.NotifyCanExecuteChanged();
    }
  } = true;

  public event PropertyChangedEventHandler? PropertyChanged;

  public Action? FullscreenAction { get; set; }

  private bool _singleTap = true; // Prevents single and double tapping event conflict
  private bool _wheelScrolled = false; // Used to disable sticky chat when scrolling with mouse wheel
  private int _currentCommentIndex = -1;
  private readonly uint _stickyChatOffset = 100; // Pixel distance where the chat will scroll automatically
  private readonly DispatcherQueue _dispatcherQueue;

  public MainPage()
  {
    InitializeComponent();

    _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    var timer = new DispatcherTimer() { Interval = new(0, 0, 2) };
    timer.Tick += Timer_Tick;
    timer.Start();

    AllowDrop = true;

    DragEnter += OnDragEnter;
    Drop += OnDrop;

    MediaPlayer.MediaPlayer.PlaybackSession.SeekCompleted += PlaybackSession_SeekCompleted;
    CommentsItemsRepeater.ElementPrepared += CommentsItemsRepeater_ElementPrepared;
    CommentsScrollViewer.AddHandler(PointerWheelChangedEvent, new PointerEventHandler(CommentsScrollViewer_PointerWheelChanged), true);
  }

  [NotNull] public RelayCommand? PanelOpenCommand => field ??= new(execute: () => { IsPaneOpen = !IsPaneOpen; });
  [NotNull]
  public RelayCommand? StickToChatCommand => field ??= new(execute: () =>
  {
    StickyChat = true;

    if (Comments.Count == 0)
      return;

    var commentElement = CommentsItemsRepeater.GetOrCreateElement(Math.Max(_currentCommentIndex, 0));

    CommentsItemsRepeater.UpdateLayout();

    commentElement.StartBringIntoView(new()
    {
      AnimationDesired = false, // Using animation will make the message position very inconsistent when multiple messages has been updated
      VerticalAlignmentRatio = 1f
    });
  });

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
      _currentCommentIndex = -1;
      StickToChatCommand.Execute(null);
    }
  }

  private bool UpdateMessages(TimeSpan position)
  {
    if (Comments.Count == 0)
      return false;

    var startIndex = _currentCommentIndex;
    var positionSeconds = (uint)position.TotalSeconds;
    var lastMessageSeconds = _currentCommentIndex != -1 ? Comments[_currentCommentIndex].OffsetSeconds : 0;

    if (positionSeconds > lastMessageSeconds)
    {
      // Forward
      var nextIndex = _currentCommentIndex + 1;

      while (nextIndex < Comments.Count)
      {
        if (Comments[nextIndex].OffsetSeconds <= positionSeconds)
        {
          Comments[nextIndex].IsSent = true;
          nextIndex = ++_currentCommentIndex + 1;
        }
        else
          break;
      }
    }
    else
    {
      // Backwards
      while (_currentCommentIndex >= 0)
      {
        if (Comments[_currentCommentIndex].OffsetSeconds > positionSeconds)
        {
          Comments[_currentCommentIndex].IsSent = false;
          _currentCommentIndex--;
        }
        else
          break;
      }
    }

    return startIndex != _currentCommentIndex;
  }

  private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
  {
    ArgumentNullException.ThrowIfNull(field);

    if (!field.Equals(value))
    {
      field = value;
      PropertyChanged?.Invoke(this, new(propertyName));

      return true;
    }

    return false;
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
    if (MediaPlayer.MediaPlayer.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Paused)
      return;

    var updated = UpdateMessages(MediaPlayer.MediaPlayer.PlaybackSession.Position);

    if (updated && StickyChat)
      StickToChatCommand.Execute(null);
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

  private async void MediaPlayer_Tapped(object sender, TappedRoutedEventArgs e)
  {
    if (e.OriginalSource is not (Border { Name: "" } or FrameworkElement { Name: "RootGrid" }))
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

  private void MediaPlayer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
  {
    _singleTap = false;

    FullscreenAction?.Invoke();

    e.Handled = true;
  }

  private void PlaybackSession_SeekCompleted(Windows.Media.Playback.MediaPlaybackSession session, object args)
  {
    _dispatcherQueue.TryEnqueue(() =>
    {
      MediaPlayer.Focus(FocusState.Programmatic); // Disable focus on the timeline slider

      if (UpdateMessages(session.Position))
        StickToChatCommand.Execute(null);
    });
  }

  private void MediaPlayerState_KeyboardAccelerator_Invoked(KeyboardAccelerator _, KeyboardAcceleratorInvokedEventArgs __)
  {
    if (MediaPlayer.MediaPlayer.CurrentState == Windows.Media.Playback.MediaPlayerState.Playing)
      MediaPlayer.MediaPlayer.Pause();
    else if (MediaPlayer.MediaPlayer.CurrentState == Windows.Media.Playback.MediaPlayerState.Paused)
      MediaPlayer.MediaPlayer.Play();
  }

  private void SeekForward_KeyboardAccelerator_Invoked(KeyboardAccelerator _, KeyboardAcceleratorInvokedEventArgs __)
    => MediaPlayer.MediaPlayer.PlaybackSession.Position += new TimeSpan(0, 0, 5);

  private void SeekBackward_KeyboardAccelerator_Invoked(KeyboardAccelerator _, KeyboardAcceleratorInvokedEventArgs __)
    => MediaPlayer.MediaPlayer.PlaybackSession.Position -= new TimeSpan(0, 0, 5);

  private void CommentsItemsRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
  {
    if (args.Element is not CommentContainer container)
      return;

    container.AddHandler(TappedEvent, new TappedEventHandler(CommentContainer_Tapped), true);
    container.AddHandler(DoubleTappedEvent, new DoubleTappedEventHandler(CommentContainer_DoubleTapped), true);
  }

  private async void CommentContainer_Tapped(object sender, TappedRoutedEventArgs e)
  {
    if ((sender as FrameworkElement)?.DataContext is not Comment comment)
      return;

    _singleTap = true;

    await Task.Delay(200);

    if (_singleTap)
      MediaPlayer.MediaPlayer.PlaybackSession.Position = new TimeSpan(0, 0, (int)comment.OffsetSeconds);
  }

  private void CommentContainer_DoubleTapped(object _, DoubleTappedRoutedEventArgs e)
    => _singleTap = false;

  private void CommentsScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    => _wheelScrolled = true;

  private void CommentsScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
  {
    // Calculate sticky chat state when scrolled with mouse wheel

    if (Comments.Count == 0 || !_wheelScrolled)
      return;

    if (CommentsItemsRepeater.TryGetElement(Math.Max(_currentCommentIndex, 0)) is not UIElement lastElement)
    {
      StickyChat = false;
      return;
    }

    var scrollBottomOffset = e.FinalView.VerticalOffset + CommentsScrollViewer.ActualHeight;
    var elementOffset = lastElement.ActualOffset.Y + lastElement.ActualSize.Y;

    if (e.FinalView.VerticalOffset == 0 && elementOffset < CommentsScrollViewer.ActualHeight)
      StickyChat = true;
    else
      StickyChat = Math.Abs(elementOffset - scrollBottomOffset) <= _stickyChatOffset;

    _wheelScrolled = !StickyChat;
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
}