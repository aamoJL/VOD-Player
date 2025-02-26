using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Diagnostics;
using System.IO;

namespace VODPlayer;

public sealed partial class MainPage : Page
{
  public MainPage()
    => InitializeComponent();

  protected override void OnNavigatedTo(NavigationEventArgs e)
  {
    base.OnNavigatedTo(e);

    if (e.Parameter is string filePath && File.Exists(filePath) && Path.GetExtension(filePath) is ".mp4")
      Debug.WriteLine("file: " + filePath);
  }
}
