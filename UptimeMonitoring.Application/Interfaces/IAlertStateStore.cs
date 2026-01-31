namespace UptimeMonitoring.Application.Interfaces;

public interface IAlertStateStore
{
    Task<bool?> GetLastStateAsync(Guid websiteId);
    Task SetStateAsync(Guid websiteId, bool isUp);
    Task DeleteStateAsync(Guid websiteId);
}
