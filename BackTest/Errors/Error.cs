using System.Text.Json.Serialization;

namespace BackTest.Errors;

public class Error
{
    public int Code { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
}