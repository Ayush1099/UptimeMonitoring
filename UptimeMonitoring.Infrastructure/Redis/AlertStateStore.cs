using StackExchange.Redis;
using UptimeMonitoring.Application.Interfaces;

namespace UptimeMonitoring.Infrastructure.Redis;

public class AlertStateStore : IAlertStateStore
{
    private readonly IDatabase _db;

    public AlertStateStore(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
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
        await _db.KeyDeleteAsync($"website:state:{websiteId}");
    }

}
