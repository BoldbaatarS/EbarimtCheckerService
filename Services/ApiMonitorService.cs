namespace EbarimtCheckerService.Services;

public class ApiMonitorService
{
    private readonly HttpClient _http;
    private readonly EmailService _email;
    private readonly ILogger<ApiMonitorService> _logger;

    public ApiMonitorService(HttpClient http, EmailService email, ILogger<ApiMonitorService> logger)
    {
        _http = http;
        _email = email;
        _logger = logger;
    }

    public async Task CheckHealthAsync()
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var url = $"https://api.ebarimt.mn/api/info/check/barcode/all?page=0&size=1&date={today}";

        try
        {
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                string msg = $"❌ API Unavailable! Status: {response.StatusCode}";
                _logger.LogWarning(msg);
                await _email.SendEmailAsync("🚨 Ebarimt API Down", msg);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "API check failed");
            await _email.SendEmailAsync("🔥 API Exception", ex.Message);
        }
    }
}
