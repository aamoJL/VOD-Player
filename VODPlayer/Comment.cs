using System.Linq;

namespace VODPlayer;

public class Comment()
{
  public class Fragment()
  {
    public string Text { get; set; } = string.Empty;
  }

  public string Commenter { get; set; } = string.Empty;
  public Fragment[] Fragments { get; set; } = [];
  public string Color { get; set; } = "#FF0000";

  public string Body => string.Join(' ', Fragments.Select(x => x.Text));
}