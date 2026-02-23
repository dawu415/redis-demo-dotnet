using System.Text.Json.Serialization;

namespace RedisDemo.Models;

public class Item
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }

    public override string ToString() =>
        $"Item{{id='{Id}', name='{Name}', description='{Description}', createdAt={CreatedAt}, updatedAt={UpdatedAt}}}";
}
