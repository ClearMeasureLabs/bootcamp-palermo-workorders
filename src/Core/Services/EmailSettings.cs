namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Configuration settings for email service.
/// </summary>
public class EmailSettings
{
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string SmtpUsername { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "Church Bulletin Work Orders";
    public string BaseUrl { get; set; } = "";
}
