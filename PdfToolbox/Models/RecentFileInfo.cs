namespace PdfToolbox.Models;

public class RecentFileInfo
{
    public string FilePath { get; set; } = "";
    public string FileName => System.IO.Path.GetFileName(FilePath);
    public string DirectoryName => System.IO.Path.GetDirectoryName(FilePath) ?? "";
}
