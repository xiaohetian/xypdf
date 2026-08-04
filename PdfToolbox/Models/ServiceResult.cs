namespace PdfToolbox.Models;

public class ServiceResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = "";

    public static ServiceResult Ok() => new() { Success = true };
    public static ServiceResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}