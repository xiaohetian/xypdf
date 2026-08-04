using System.Runtime.InteropServices;

namespace PdfToolbox.Helpers;

public static class FolderBrowserHelper
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out string pszPath);

    public static string? SelectFolder(string description = "选择文件夹")
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = description,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "选择文件夹"
            };

            if (dialog.ShowDialog() == true)
            {
                return System.IO.Path.GetDirectoryName(dialog.FileName);
            }
        }
        catch
        {
        }
        return null;
    }
}