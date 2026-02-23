using RedisDemo.Models;
using StackExchange.Redis;
using Steeltoe.Connectors;
using Steeltoe.Connectors.Redis;

namespace RedisDemo.Repositories;

/// <summary>
/// Manual repository using Redis hashes — equivalent to Spring Data Redis @RedisHash("items").
/// Key pattern: "items:{id}" for individual hashes, "items" as the tracking set.
/// </summary>
public class ItemRepository
{
    private const string KeyPrefix = "items";
    private const string IndexSetKey = "items";

    private readonly ConnectorFactory<RedisOptions, IConnectionMultiplexer> _connectorFactory;

    public ItemRepository(ConnectorFactory<RedisOptions, IConnectionMultiplexer> connectorFactory)
    {
        _connectorFactory = connectorFactory;
    }

    private IDatabase Db => GetMultiplexer().GetDatabase();

    public IConnectionMultiplexer GetMultiplexer() =>
        _connectorFactory.Get().GetConnection();

    public async Task<Item> SaveAsync(Item item)
    {
        if (string.IsNullOrEmpty(item.Id))
        {
            item.Id = Guid.NewGuid().ToString();
        }

        var key = $"{KeyPrefix}:{item.Id}";
        var entries = new HashEntry[]
        {
            new("id", item.Id),
            new("name", item.Name),
            new("description", item.Description),
            new("createdAt", item.CreatedAt.ToString("O")),
            new("updatedAt", item.UpdatedAt.ToString("O"))
        };

        await Db.HashSetAsync(key, entries);
        await Db.SetAddAsync(IndexSetKey, item.Id);

        return item;
    }

    public async Task<Item?> FindByIdAsync(string id)
    {
        var key = $"{KeyPrefix}:{id}";
        var entries = await Db.HashGetAllAsync(key);

        if (entries.Length == 0) return null;

        return MapToItem(entries);
    }

    public async Task<List<Item>> FindAllAsync()
    {
        var ids = await Db.SetMembersAsync(IndexSetKey);
        var items = new List<Item>();

        foreach (var id in ids)
        {
            var item = await FindByIdAsync(id.ToString());
            if (item != null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    public async Task<List<Item>> FindByNameAsync(string name)
    {
        var all = await FindAllAsync();
        return all.Where(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<bool> ExistsAsync(string id)
    {
        var key = $"{KeyPrefix}:{id}";
        return await Db.KeyExistsAsync(key);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var key = $"{KeyPrefix}:{id}";
        var deleted = await Db.KeyDeleteAsync(key);
        await Db.SetRemoveAsync(IndexSetKey, id);
        return deleted;
    }

    public async Task<long> CountAsync()
    {
        return await Db.SetLengthAsync(IndexSetKey);
    }

    private static Item MapToItem(HashEntry[] entries)
    {
        var dict = entries.ToDictionary(
            e => e.Name.ToString(),
            e => e.Value.ToString());

        return new Item
        {
            Id = dict.GetValueOrDefault("id", ""),
            Name = dict.GetValueOrDefault("name", ""),
            Description = dict.GetValueOrDefault("description", ""),
            CreatedAt = DateTimeOffset.TryParse(dict.GetValueOrDefault("createdAt"), out var c) ? c : DateTimeOffset.MinValue,
            UpdatedAt = DateTimeOffset.TryParse(dict.GetValueOrDefault("updatedAt"), out var u) ? u : DateTimeOffset.MinValue
        };
    }
}
