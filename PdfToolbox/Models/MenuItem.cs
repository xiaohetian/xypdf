using System.Windows.Media;

namespace PdfToolbox.Models;

public class MenuItem
{
    public string Label { get; set; } = "";
    public string CommandParameter { get; set; } = "";
    public Geometry? Icon { get; set; }
    public Brush? IconColor { get; set; }
}
