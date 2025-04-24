using System.Drawing;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace YoutubeChatConverter;

internal class Program
{
  static void Main(string[] _)
  {
    var filePath = Environment.GetCommandLineArgs().FirstOrDefault(arg =>
    {
      try
      {
        if (string.IsNullOrEmpty(arg) || !File.Exists(arg))
          throw new FileNotFoundException();

        if (Path.GetExtension(arg) is not ".json")
          throw new FileNotFoundException("File extension needs to be .json");

        return true;
      }
      catch (Exception e)
      {
        LogError(e.Message);
      }

      return false;
    });

    if (filePath == null)
    {
      LogError("File path is null.");
      Terminate();
    }

    Console.WriteLine("File: " + filePath);

    var nonCommentCount = 0;
    var comments = new List<YoutubeComment>();

    try
    {
      if (string.IsNullOrEmpty(filePath))
        throw new FileNotFoundException();

      var jsons = File.ReadLines(filePath).Select(x => JsonNode.Parse(x));

      foreach (var json in jsons)
      {
        if (json?["replayChatItemAction"]?.AsObject() is JsonObject replayAction)
        {
          if (ParseReplayChatItemAction(replayAction) is YoutubeComment comment)
            comments.Add(comment);
          else
            nonCommentCount++;
        }
      }
    }
    catch (Exception e)
    {
      LogError(e.Message);
      Terminate();
    }

    Console.WriteLine("Comments: " + comments.Count);
    Console.WriteLine("Other: " + nonCommentCount);

    var serializedJson = JsonSerializer.Serialize(new
    {
      comments = comments.Select(c => new
      {
        commenter = new { display_name = c.Author },
        message = new
        {
          user_color = c.GetColor(),
          fragments = c.Runs.Select(r => new { text = r.Text })
        },
        content_offset_seconds = c.OffsetMS / 1000
      }),
    });

    var newFileName = Path.GetFileNameWithoutExtension(filePath) + " - Converted.json";
    var newFilePath = Path.Join(Path.GetDirectoryName(filePath), newFileName);

    if (File.Exists(newFilePath))
    {
      LogError("Error: Converted file already exists.");
      Terminate();
    }
    else
    {
      File.WriteAllText(newFilePath, serializedJson);
      Console.WriteLine("Completed.");
    }

    Console.WriteLine("Press any key to close the window.");
    Console.ReadKey();
  }

  private static YoutubeComment? ParseReplayChatItemAction(JsonObject replayAction)
  {
    try
    {
      if (replayAction["actions"]?.AsArray().FirstOrDefault()?.AsObject()["addChatItemAction"]?["item"]?["liveChatTextMessageRenderer"] is JsonObject comment)
      {
        if (!int.TryParse(replayAction["videoOffsetTimeMsec"]?.GetValue<string>(), out var offset))
          throw new Exception("Offset is null");

        var author = comment["authorName"]?["simpleText"]?.GetValue<string>() ?? throw new Exception("Author is null");
        var runs = comment["message"]?["runs"]?.AsArray()?.Select(x => new YoutubeComment.Run(x?["text"]?.GetValue<string>() ?? "")).ToArray() ?? throw new Exception("Runs is null");

        return new(author, runs, offset);
      }
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
    }

    return null;
  }

  public static void LogError(string message)
  {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(message);
    Console.ResetColor();
  }

  public static void Terminate()
  {
    Console.WriteLine("Press any key to close the window.");
    Console.ReadKey();
    Environment.Exit(0);
  }
}

public record class YoutubeComment(string Author, YoutubeComment.Run[] Runs, int OffsetMS)
{
  private static readonly Color[] AvailableColors = [
    Color.AliceBlue, Color.Blue, Color.Brown, Color.Cyan, Color.DarkRed,
    Color.Red, Color.Green, Color.Yellow, Color.Violet, Color.Pink];

  public static readonly Dictionary<string, string> Colors = [];

  public record class Run(string Text);

  /// <summary>
  /// Generates or returns a random color for the author. The color will be stored in the <see cref="Colors"/> dictionary.
  /// </summary>
  /// <returns>Authors color in six char hex format. For example: #010BFF </returns>
  public string GetColor()
  {
    if (Colors.TryGetValue(Author, out var color))
      return color;
    else
    {
      var newColor = AvailableColors[new Random().Next(AvailableColors.Length)];
      var newColorString = $"#{newColor.R:X2}{newColor.G:X2}{newColor.B:X2}";

      Colors.Add(Author, newColorString);

      return newColorString;
    }
  }
};
