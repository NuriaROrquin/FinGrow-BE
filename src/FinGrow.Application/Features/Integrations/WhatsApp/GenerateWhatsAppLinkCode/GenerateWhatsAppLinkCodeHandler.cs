namespace FinGrow.Application.Features.Integrations.WhatsApp.GenerateWhatsAppLinkCode;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class GenerateWhatsAppLinkCodeHandler
    : IRequestHandler<GenerateWhatsAppLinkCodeCommand, Result<WhatsAppLinkCodeResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeRepository _employees;
    private readonly IIntegrationLinkCodeRepository _linkCodes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public GenerateWhatsAppLinkCodeHandler(
        ICurrentUser currentUser,
        IEmployeeRepository employees,
        IIntegrationLinkCodeRepository linkCodes,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _employees = employees;
        _linkCodes = linkCodes;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<WhatsAppLinkCodeResponse>> Handle(
        GenerateWhatsAppLinkCodeCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<WhatsAppLinkCodeResponse>(
                Error.Forbidden("Integrations.Unauthenticated", "Hay que iniciar sesion para vincular WhatsApp."));
        }

        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            return Result.Failure<WhatsAppLinkCodeResponse>(
                Error.Forbidden("Integrations.NotAnEmployee", "Solo un empleado activo puede vincular WhatsApp."));
        }

        var code = IntegrationLinkCode.GenerateCode();
        var linkCode = IntegrationLinkCode.Create(employee.Id, IntegrationProvider.WhatsApp, code, _clock.UtcNow);

        _linkCodes.Add(linkCode);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new WhatsAppLinkCodeResponse(code, linkCode.ExpiresAt));
    }
}
