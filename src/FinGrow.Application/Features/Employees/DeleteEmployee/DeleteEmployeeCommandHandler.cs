namespace FinGrow.Application.Features.Employees.DeleteEmployee;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class DeleteEmployeeCommandHandler : IRequestHandler<DeleteEmployeeCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeRepository _employees;
    private readonly IUserRepository _users;
    private readonly ITransactionReceiptRepository _receipts;
    private readonly INotificationRepository _notifications;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IFileStorage _fileStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<DeleteEmployeeCommandHandler> _logger;

    public DeleteEmployeeCommandHandler(
        ICurrentUser currentUser,
        IEmployeeRepository employees,
        IUserRepository users,
        ITransactionReceiptRepository receipts,
        INotificationRepository notifications,
        IRefreshTokenRepository refreshTokens,
        IFileStorage fileStorage,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<DeleteEmployeeCommandHandler> logger)
    {
        _currentUser = currentUser;
        _employees = employees;
        _users = users;
        _receipts = receipts;
        _notifications = notifications;
        _refreshTokens = refreshTokens;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.CompanyId is not { } companyId)
        {
            return Result.Failure(EmployeeErrors.NoAutenticado);
        }

        var employee = await _employees.GetByIdAsync(request.EmployeeId, cancellationToken);

        if (employee is null || employee.CompanyId != companyId)
        {
            return Result.Failure(EmployeeErrors.NotFound(request.EmployeeId));
        }

        var receiptKeys = await _receipts.ListStorageKeysByEmployeeAsync(employee.Id, cancellationToken);

        _employees.Remove(employee);
        _users.Remove(employee.User);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _refreshTokens.RevokeAllForUserAsync(employee.Id, _clock.UtcNow, cancellationToken);
        await _notifications.RemoveAllForAsync(NotificationRecipient.Employee(employee.Id), cancellationToken);
        await DeleteReceiptFilesAsync(employee.Id, receiptKeys, cancellationToken);

        return Result.Success();
    }

    private async Task DeleteReceiptFilesAsync(
        Guid employeeId,
        IReadOnlyList<string> receiptKeys,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < receiptKeys.Count; index++)
        {
            try
            {
                await _fileStorage.DeleteAsync(receiptKeys[index], cancellationToken);
            }
            catch (FileStorageUnavailableException exception)
            {
                LogReceiptFilesLeftBehind(_logger, exception, employeeId, receiptKeys.Count - index);
                return;
            }
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Se elimino el empleado {EmployeeId} pero el almacenamiento no respondio: quedaron {Files} comprobante(s) sin borrar.")]
    private static partial void LogReceiptFilesLeftBehind(ILogger logger, Exception exception, Guid employeeId, int files);
}
