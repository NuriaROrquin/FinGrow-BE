namespace FinGrow.Application.Interfaces;

public interface IAiService
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
