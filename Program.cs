using Microsoft.EntityFrameworkCore;
using EbarimtCheckerService.Data;
using EbarimtCheckerService.Models;
using EbarimtCheckerService.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// ⚙️ Config services
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<ApiMonitorService>();
builder.Services.AddScoped<DataSyncService>();

// 🧠 Worker background service
builder.Services.AddHostedService<Worker>();

// 👇  Энэ хэсгийг нэмнэ:
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate(); // ⚡️ автоматаар schema үүсгэнэ
}

// ---------------------------
// 📍 REST API Endpoint
// ---------------------------

// Get all products
app.MapGet("/products", async (AppDbContext db) =>
{
    return await db.Products.ToListAsync();
});

// Get product by barcode
//https://electromon.com/products/8028338990956
app.MapGet("/products/{barcode}", async (string barcode, AppDbContext db) =>
{
    var product = await db.Products.FirstOrDefaultAsync(p => p.BarCode == barcode);
    return product is not null ? Results.Ok(product) : Results.NotFound();
});
// POST by Batch product lookup
app.MapPost("/products/batch", async ([FromBody] List<string> barcodes, AppDbContext db) =>
{
    var products = await db.Products
        .Where(p => barcodes.Contains(p.BarCode))
        .ToListAsync();

    return Results.Ok(products);
});
// Get products with pagination and optional date filter
// https://electromon.com/products/page/1?size=50&date=2025-10-13
app.MapGet("/products/page/{page:int}", async (int page, int? size, DateTime? date, AppDbContext db) =>
{
    int pageSize = size ?? 10;
    if (page < 1) page = 1;

    // ✅ 1️⃣ Filter — зөвхөн тухайн өдөрт хамаарах бичлэгүүд
    IQueryable<Product> query = db.Products;

    if (date.HasValue)
    {
        query = query.Where(p => p.Date.Date == date.Value.Date);
    }

    // ✅ 2️⃣ Filter хийсний дараа тоолох
    int totalItems = await query.CountAsync();
    int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

    // ✅ 3️⃣ Page-ийн өгөгдөл авах
    var items = await query
        .OrderBy(p => p.BarCode)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    // ✅ 4️⃣ Хариу JSON
    var response = new
    {
        currentPage = page,
        pageSize = pageSize,
        totalPages = totalPages,
        totalItems = totalItems,
        filterDate = date?.ToString("yyyy-MM-dd") ?? "All",
        items = items
    };

    return Results.Ok(response);
});
// Get products updated after a specific date with pagination
// https://electromon.com/products/updates?fromDate=2025-10-13&size=50&page=1
app.MapGet("/products/updates", async (DateTime fromDate, int? size, int? page, AppDbContext db) =>
{
    int pageSize = size ?? 100;
    int currentPage = page ?? 1;

    if (currentPage < 1) currentPage = 1;

    // 🧠 1️⃣ fromDate-с хойших бүх бүтээгдэхүүн
    IQueryable<Product> query = db.Products
        .Where(p => p.Date > fromDate);

    int totalItems = await query.CountAsync();
    int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

    var items = await query
        .OrderBy(p => p.Date)
        .ThenBy(p => p.BarCode)
        .Skip((currentPage - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    // 🧠 2️⃣ Хариу бүтэц
    var response = new
    {
        fromDate = fromDate.ToString("yyyy-MM-dd"),
        currentPage = currentPage,
        pageSize = pageSize,
        totalPages = totalPages,
        totalItems = totalItems,
        newestDate = items.Any() ? items.Max(p => p.Date).ToString("yyyy-MM-dd") : fromDate.ToString("yyyy-MM-dd"),
        items = items
    };

    return Results.Ok(response);
});

// Root endpoint
app.MapGet("/", () => "EbarimtCheckerService API is running 🚀");

// ---------------------------

app.Run();
