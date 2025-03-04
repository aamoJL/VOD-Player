using System.ComponentModel;
using System.Linq;

namespace VODPlayer;

public partial class Comment() : INotifyPropertyChanged
{
  public class Fragment()
  {
    public string Text { get; set; } = string.Empty;
  }

  public string Commenter { get; set; } = string.Empty;
  public Fragment[] Fragments { get; set; } = [];
  public string Color { get; set; } = "#FF0000";
  public uint OffsetSeconds { get; set; } = 0;

  public bool IsSent
  {
    get;
    set
    {
      if (field != value)
      {
        field = value;
        PropertyChanged?.Invoke(this, new(nameof(IsSent)));
      }
    }
  } = false;

  public string Body => string.Join(' ', Fragments.Select(x => x.Text));

  public event PropertyChangedEventHandler? PropertyChanged;
}