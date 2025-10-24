using System.ComponentModel.DataAnnotations;

namespace EbarimtCheckerService.Models;

public class Product
{
    [Key]
    public string BarCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTime Date { get; set; }
    public string BunaCode { get; set; } = default!;
}
