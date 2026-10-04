namespace FinGrow.Application.Features.Account.Profile;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result>
{
    private static readonly Error NoAutenticado =
        Error.Unauthorized("Account.NoAutenticado", "Hay que iniciar sesión para editar tu perfil.");

    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public UpdateProfileCommandHandler(
        ICurrentUser currentUser,
        IEmployeeRepository employees,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _employees = employees;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } employeeId)
        {
            return Result.Failure(NoAutenticado);
        }

        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);

        if (employee is null)
        {
            return Result.Failure(NoAutenticado);
        }

        employee.UpdateProfile(
            request.FullName,
            request.PhoneNumber,
            request.NationalId,
            request.BirthDate,
            request.Address,
            _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
