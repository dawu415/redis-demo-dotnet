using Microsoft.AspNetCore.Mvc;
using RedisDemo.Models;
using RedisDemo.Repositories;

namespace RedisDemo.Controllers;

[ApiController]
[Route("api/items")]
public class ItemController : ControllerBase
{
    private readonly ItemRepository _repository;

    public ItemController(ItemRepository repository)
    {
        _repository = repository;
    }

    // ---- CRUD Endpoints ----

    /// <summary>
    /// CREATE - Add a new item.
    /// POST /api/items
    /// Body: { "name": "test-key", "description": "some value" }
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Item item)
    {
        item.CreatedAt = DateTimeOffset.UtcNow;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        var saved = await _repository.SaveAsync(item);
        return StatusCode(201, saved);
    }

    /// <summary>
    /// READ - Get a single item by ID.
    /// GET /api/items/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var item = await _repository.FindByIdAsync(id);
        return item is not null ? Ok(item) : NotFound();
    }

    /// <summary>
    /// READ - Search items by name.
    /// GET /api/items/search?name=test-key
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchByName([FromQuery] string name)
    {
        var items = await _repository.FindByNameAsync(name);
        return Ok(items);
    }

    /// <summary>
    /// UPDATE - Update an existing item.
    /// PUT /api/items/{id}
    /// Body: { "name": "updated-key", "description": "updated value" }
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] Item item)
    {
        var existing = await _repository.FindByIdAsync(id);
        if (existing is null) return NotFound();

        existing.Name = item.Name;
        existing.Description = item.Description;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        var saved = await _repository.SaveAsync(existing);
        return Ok(saved);
    }

    /// <summary>
    /// DELETE - Remove an item by ID.
    /// DELETE /api/items/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var exists = await _repository.ExistsAsync(id);
        if (!exists) return NotFound();

        await _repository.DeleteAsync(id);
        return NoContent();
    }

    // ---- Get All ----

    /// <summary>
    /// LIST ALL - Retrieve every item in the store.
    /// GET /api/items
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _repository.FindAllAsync();
        return Ok(items);
    }

    // ---- Lazy Create (GET-based) ----

    /// <summary>
    /// CREATE via GET - The lazy way.
    /// GET /api/items/create?id=1&amp;name=foo&amp;desc=bar
    /// </summary>
    [HttpGet("create")]
    public async Task<IActionResult> PutWithGet(
        [FromQuery] int id,
        [FromQuery] string name,
        [FromQuery] string desc)
    {
        var item = new Item
        {
            Id = id.ToString(),
            Name = name,
            Description = desc,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var saved = await _repository.SaveAsync(item);
        return StatusCode(201, saved);
    }

    // ---- Diagnostics ----

    /// <summary>
    /// CONNECTION INFO - Validates which backing service you're connected to.
    /// GET /api/items/info
    /// </summary>
    [HttpGet("info")]
    public async Task<IActionResult> ConnectionInfo()
    {
        var info = new Dictionary<string, object>();
        try
        {
            var redis = _repository.GetMultiplexer();
            var server = redis.GetServer(redis.GetEndPoints().First());
            var serverInfo = await server.InfoAsync("server");

            info["status"] = "CONNECTED";
            info["endpoint"] = redis.GetEndPoints().First().ToString()!;

            var serverSection = serverInfo
                .SelectMany(g => g)
                .ToDictionary(p => p.Key, p => p.Value);

            info["redis_version"] = serverSection.GetValueOrDefault("redis_version", "unknown");
            info["redis_mode"] = serverSection.GetValueOrDefault("redis_mode", "unknown");
            info["os"] = serverSection.GetValueOrDefault("os", "unknown");
            info["tcp_port"] = serverSection.GetValueOrDefault("tcp_port", "unknown");
            info["uptime_in_seconds"] = serverSection.GetValueOrDefault("uptime_in_seconds", "unknown");
            info["server_name"] = serverSection.GetValueOrDefault("server_name", "redis");

            info["item_count"] = await _repository.CountAsync();
        }
        catch (Exception ex)
        {
            info["status"] = "ERROR";
            info["error"] = ex.Message;
        }

        return Ok(info);
    }
}
