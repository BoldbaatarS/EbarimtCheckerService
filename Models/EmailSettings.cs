namespace EbarimtCheckerService.Models;

public class EmailSettings
{
    public required string FromName { get; set; }
    public required string FromEmail { get; set; }
    public required string ToName { get; set; }
    public required string ToEmail { get; set; }
    public required string SmtpHost { get; set; }
    public required int SmtpPort { get; set; }
    public required string SmtpUser { get; set; }
    public required string SmtpPass { get; set; }
}
