using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Linq;

namespace VODPlayer;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
  /// <summary>
  /// Initializes the singleton application object.  This is the first line of authored code
  /// executed, and as such is the logical equivalent of main() or WinMain().
  /// </summary>
  public App()
  {
    RequestedTheme = ApplicationTheme.Dark;

    InitializeComponent();
  }

  /// <summary>
  /// Invoked when the application is launched.
  /// </summary>
  /// <param name="args">Details about the launch request and process.</param>
  protected override void OnLaunched(LaunchActivatedEventArgs args)
  {
    var droppedFilePath = Environment.GetCommandLineArgs().FirstOrDefault(arg =>
    {
      try
      {
        if (File.Exists(arg) && Path.GetExtension(arg) is ".mp4")
          return true;
      }
      catch { }

      return false;
    });

    new MainWindow(droppedFilePath).Activate();
  }
}
