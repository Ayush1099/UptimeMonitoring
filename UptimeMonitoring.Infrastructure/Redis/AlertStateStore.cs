using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using UptimeMonitoring.Application.Interfaces;

namespace UptimeMonitoring.Infrastructure.Redis;

public class AlertStateStore : IAlertStateStore
{
    private readonly IDatabase _db;
    private readonly ILogger<AlertStateStore> _logger;

    public AlertStateStore(IConnectionMultiplexer redis, ILogger<AlertStateStore> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    private static string GetKey(Guid websiteId)
        => $"website:state:{websiteId}";

    public async Task<bool?> GetLastStateAsync(Guid websiteId)
    {
        var value = await _db.StringGetAsync(GetKey(websiteId));
        if (value.IsNull)
            return null;

        return value == "UP";
    }

    public async Task SetStateAsync(Guid websiteId, bool isUp)
    {
        await _db.StringSetAsync(
            GetKey(websiteId),
            isUp ? "UP" : "DOWN"
        );
    }
    public async Task DeleteStateAsync(Guid websiteId)
    {
        try
        {
            await _db.KeyDeleteAsync(GetKey(websiteId));
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis connection unavailable while deleting state for website {WebsiteId}; allowing delete to succeed without cleaning cache", websiteId);
        }
        catch (RedisTimeoutException ex)
        {
            _logger.LogWarning(ex, "Redis timeout while deleting state for website {WebsiteId}; allowing delete to succeed", websiteId);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error while deleting state for website {WebsiteId}; allowing delete to succeed", websiteId);
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogWarning(ex, "Redis connection disposed while deleting state for website {WebsiteId}; allowing delete to succeed", websiteId);
        }
    }

}
