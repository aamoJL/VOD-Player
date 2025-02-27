using Microsoft.UI.Xaml;

namespace VODPlayer;

public sealed partial class MainWindow : Window
{
  public MainWindow(string? filePath = null)
  {
    InitializeComponent();

    MainFrame.Navigate(typeof(MainPage), filePath);

    if (MainFrame.Content is MainPage page)
      page.FullscreenAction = () =>
      {
        if (AppWindow.Presenter.Kind == Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen)
          AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Default);
        else
          AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
      };
  }

  private void ExitFullScreenKeyboardAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
  {
    if (AppWindow.Presenter.Kind == Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen)
      AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Default);
  }
}
