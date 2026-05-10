namespace EcoMonitor.Application.Common.Models;

public sealed class EmailOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public int MaxRetryAttempts { get; set; } = 5;
    public int RetryDelayMinutes { get; set; } = 2;
}
