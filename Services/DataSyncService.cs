using System.Net.Http.Json;
using EbarimtCheckerService.Data;
using EbarimtCheckerService.Models;
using Microsoft.EntityFrameworkCore;

namespace EbarimtCheckerService.Services;

public class DataSyncService
{
    private readonly HttpClient _http;
    private readonly AppDbContext _db;
    private readonly ILogger<DataSyncService> _logger;

    public DataSyncService(HttpClient http, AppDbContext db, ILogger<DataSyncService> logger)
    {
        _http = http;
        _db = db;
        _logger = logger;
    }

    public async Task FetchAndSaveNewDataAsync()
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        int page = 0;
        int totalInserted = 0;

        _logger.LogInformation("📡 Starting data sync for date {date}", today);

        while (true)
        {
            var url = $"https://api.ebarimt.mn/api/info/check/barcode/all?page={page}&size=200&date={today}";
            _logger.LogInformation("➡️ Fetching page {page}", page);

            ApiResponse? response = null;
            try
            {
                response = await _http.GetFromJsonAsync<ApiResponse>(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ Failed to fetch page {page}", page);
                break;
            }

            if (response == null || response.Content == null || response.Content.Count == 0)
            {
                _logger.LogInformation("No data found on page {page}", page);
                break;
            }

            foreach (var row in response.Content)
            {
                try
                {
                    if (row.Count < 4) continue;

                    var product = new Product
                    {
                        BarCode = row[0]?.ToString() ?? "",
                        Name = row[1]?.ToString() ?? "",
                        Date = DateTime.Parse(row[2]?.ToString() ?? DateTime.UtcNow.ToString()),
                        BunaCode = row[3]?.ToString() ?? ""
                    };

                    bool exists = await _db.Products
                        .AnyAsync(p => p.BarCode == product.BarCode && p.Date == product.Date);

                    if (!exists)
                    {
                        await _db.Products.AddAsync(product);
                        totalInserted++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "❗ Error parsing row on page {page}", page);
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("✅ Page {page} processed, total new products: {count}", page, totalInserted);

            page++;
            if (page >= response.TotalPages)
            {
                _logger.LogInformation("🎯 All pages processed");
                break;
            }

            await Task.Delay(500); // API-руу хэт хурдан хүсэлт илгээхээс сэргийлнэ
        }

        _logger.LogInformation("🎉 Sync complete. Total new products added: {count}", totalInserted);
    }
}
