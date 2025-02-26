using Microsoft.UI.Xaml;

namespace VODPlayer;

public sealed partial class MainWindow : Window
{
  public MainWindow(string? filePath = null)
  {
    InitializeComponent();

    MainFrame.Navigate(typeof(MainPage), filePath);
  }
}
