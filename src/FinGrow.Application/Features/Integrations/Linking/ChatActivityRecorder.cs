namespace FinGrow.Application.Features.Integrations.Linking;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;

internal sealed class ChatActivityRecorder
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public ChatActivityRecorder(IUnitOfWork unitOfWork, IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task RecordAsync(EmployeeIntegration integration, CancellationToken cancellationToken)
    {
        integration.MarkSynced(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
