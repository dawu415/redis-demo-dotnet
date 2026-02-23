using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Steeltoe.Connectors.Redis;

namespace RedisDemo.Controllers;

/// <summary>
/// Temporary diagnostic controller to inspect config paths.
/// REMOVE before production — exposes sensitive config info.
/// </summary>
[ApiController]
[Route("api/diag")]
public class DiagController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IOptionsMonitor<RedisOptions> _options;
    public DiagController(IConfiguration config, IOptionsMonitor<RedisOptions> options)
    {
        _config = config;
        _options = options;
    }

    /// <summary>
    /// Dumps all vcap:services config keys and the Steeltoe Redis config path.
    /// GET /api/diag/config
    /// </summary>
    [HttpGet("config")]
    public IActionResult DumpConfig()
    {
        var result = new Dictionary<string, string?>();

        // Show all vcap:services keys so we can see where CredHub creds landed
        foreach (var kvp in _config.AsEnumerable()
                     .Where(k => k.Key.StartsWith("vcap:services", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(k => k.Key))
        {
            // Redact anything that looks like a password
            var value = kvp.Value;
            if (kvp.Key.Contains("password", StringComparison.OrdinalIgnoreCase) && value is not null)
                value = value[..Math.Min(3, value.Length)] + "***REDACTED***";

            result[kvp.Key] = value;
        }


        // Steeltoe uses named options; there is no public “list names” API,
        // so we test the likely candidates.
        var candidates = new[]
        {
            "Default",
            "redis-dotnet-creds-2",
            "redis",
            "credhub",
            "" // sometimes empty/default name
        };

        foreach (var name in candidates)
        {
            try
            {
                var opt = _options.Get(name);
                result[name] = opt.ConnectionString;
            }
            catch (Exception e)
            {
                result[name] = $"(error: {e.GetType().Name})";
            }
        }

        result["RedisOptions::ConnectionString"] = _options.Get("Default").ConnectionString;

        // Show what Steeltoe Redis connector will actually use
        var connStr = _config["Steeltoe:Client:Redis:Default:ConnectionString"];
        result["[RESOLVED] Steeltoe:Client:Redis:Default:ConnectionString"] =
            connStr is not null
                ? System.Text.RegularExpressions.Regex.Replace(connStr, @"password=[^,]*", "password=***REDACTED***")
                : "(null - NOT SET)";

        return Ok(result);
    }
}
