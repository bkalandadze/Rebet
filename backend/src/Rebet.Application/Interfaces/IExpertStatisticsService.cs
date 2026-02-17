namespace Rebet.Application.Interfaces;

public interface IExpertStatisticsService
{
    Task RecalculateStatisticsAsync(Guid expertId, CancellationToken cancellationToken = default);
}

