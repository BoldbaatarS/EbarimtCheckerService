using System.Text.Json.Serialization;

namespace EbarimtCheckerService.Models;

public class ApiResponse
{
    [JsonPropertyName("content")]
    public List<List<object>> Content { get; set; } = new();

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("totalElements")]
    public int TotalElements { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }
}
