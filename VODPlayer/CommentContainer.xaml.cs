using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace VODPlayer;

public partial class CommentContainer : UserControl, INotifyPropertyChanged
{
  public static readonly DependencyProperty CommenterProperty =
      DependencyProperty.Register(nameof(Commenter), typeof(string), typeof(CommentContainer), new PropertyMetadata(string.Empty, OnDependencyPropertyChanged));

  public static readonly DependencyProperty FragmentsProperty =
      DependencyProperty.Register(nameof(Fragments), typeof(IEnumerable<Comment.Fragment>), typeof(CommentContainer), new PropertyMetadata(System.Array.Empty<Comment.Fragment>(), OnDependencyPropertyChanged));

  public static readonly DependencyProperty CommenterForegroundBrushProperty =
      DependencyProperty.Register(nameof(CommenterForegroundBrush), typeof(Brush), typeof(CommentContainer), new PropertyMetadata(new SolidColorBrush(Windows.UI.Color.FromArgb(255, 128, 128, 128)), OnDependencyPropertyChanged));

  public static readonly DependencyProperty IsSentProperty =
      DependencyProperty.Register(nameof(IsSent), typeof(bool), typeof(CommentContainer), new PropertyMetadata(false, OnDependencyPropertyChanged));

  public CommentContainer()
  {
    InitializeComponent();

    if (Application.Current.Resources.TryGetValue("TextFillColorDisabledBrush", out var notSentBrush))
      _notSentTextForegroundBrush = (Brush)notSentBrush;

    if (Application.Current.Resources.TryGetValue("TextFillColorPrimaryBrush", out var sentBrush))
      _sentTextForegroundBrush = (Brush)sentBrush;

    VisualStateManager.GoToState(this, nameof(Normal), true);
  }

  public string Commenter
  {
    get => (string)GetValue(CommenterProperty);
    set => SetValue(CommenterProperty, value);
  }
  public IEnumerable<Comment.Fragment> Fragments
  {
    get => (IEnumerable<Comment.Fragment>)GetValue(FragmentsProperty);
    set => SetValue(FragmentsProperty, value);
  }
  public Brush CommenterForegroundBrush
  {
    get => (Brush)GetValue(CommenterForegroundBrushProperty);
    set => SetValue(CommenterForegroundBrushProperty, value);
  }
  public bool IsSent
  {
    get => (bool)GetValue(IsSentProperty);
    set => SetValue(IsSentProperty, value);
  }

  protected string MessageBody => string.Join(' ', Fragments.Select(x => x.Text));
  protected Brush? CommentTextBlockForegroundBrush => IsSent ? _sentTextForegroundBrush : _notSentTextForegroundBrush;
  protected Brush? CommenterTextForegroundBrush => IsSent ? CommenterForegroundBrush : _notSentTextForegroundBrush;

  public event PropertyChangedEventHandler? PropertyChanged;

  protected readonly Brush? _notSentTextForegroundBrush;
  protected readonly Brush? _sentTextForegroundBrush;

  public void NotifyPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new(propertyName));

  protected override void OnPointerEntered(PointerRoutedEventArgs e)
  {
    base.OnPointerEntered(e);
    VisualStateManager.GoToState(this, nameof(PointerOver), true);
  }

  protected override void OnPointerExited(PointerRoutedEventArgs e)
  {
    base.OnPointerExited(e);
    VisualStateManager.GoToState(this, nameof(Normal), true);
  }

  private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is not CommentContainer @this)
      return;

    if (e.Property == CommenterForegroundBrushProperty)
      @this.NotifyPropertyChanged(nameof(CommenterTextForegroundBrush));
    if (e.Property == FragmentsProperty)
      @this.NotifyPropertyChanged(nameof(@this.MessageBody));
    if (e.Property == IsSentProperty)
    {
      @this.NotifyPropertyChanged(nameof(CommenterTextForegroundBrush));
      @this.NotifyPropertyChanged(nameof(CommentTextBlockForegroundBrush));
    }
  }
}
