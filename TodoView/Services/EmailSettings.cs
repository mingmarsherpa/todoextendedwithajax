namespace TodoView.Services;

public class EmailSettings
{
    // Your Resend API key — set via env var or appsettings
    public string ApiKey { get; set; } = string.Empty;

    // Must be a verified sender address in your Resend account
    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;
}