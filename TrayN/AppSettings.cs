using System.Drawing;
using System.Text.Json.Serialization;

namespace TrayN;

internal sealed class AppSettings
{
    public int? WindowX { get; set; }
    public int? WindowY { get; set; }
    public int WindowWidth { get; set; } = 500;
    public int WindowHeight { get; set; } = 350;
    public DateTimeOffset? LastUpdateCheckUtc { get; set; }

    [JsonIgnore]
    public Rectangle? SavedBounds
    {
        get
        {
            if (WindowX is null || WindowY is null)
            {
                return null;
            }

            return new Rectangle(WindowX.Value, WindowY.Value, Math.Max(320, WindowWidth), Math.Max(220, WindowHeight));
        }
    }

    public void SetBounds(Rectangle bounds)
    {
        WindowX = bounds.X;
        WindowY = bounds.Y;
        WindowWidth = bounds.Width;
        WindowHeight = bounds.Height;
    }
}
