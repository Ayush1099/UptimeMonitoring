namespace UptimeMonitoring.Application.Interfaces;

public interface IAlertStateStore
{
    Task DeleteStateAsync(Guid websiteId);
}
