namespace FinGrow.Application.Features.Integrations.WhatsApp.GenerateWhatsAppLinkCode;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class GenerateWhatsAppLinkCodeHandler(
    ICurrentUser currentUser,
    IEmployeeRepository employees,
    IIntegrationLinkCodeRepository linkCodes,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<GenerateWhatsAppLinkCodeCommand, Result<WhatsAppLinkCodeResponse>>
{
    public async Task<Result<WhatsAppLinkCodeResponse>> Handle(
        GenerateWhatsAppLinkCodeCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<WhatsAppLinkCodeResponse>(
                Error.Forbidden("Integrations.Unauthenticated", "Hay que iniciar sesion para vincular WhatsApp."));
        }

        var employee = await employees.GetByIdAsync(employeeId, cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            return Result.Failure<WhatsAppLinkCodeResponse>(
                Error.Forbidden("Integrations.NotAnEmployee", "Solo un empleado activo puede vincular WhatsApp."));
        }

        var code = IntegrationLinkCode.GenerateCode();
        var linkCode = IntegrationLinkCode.Create(employee.Id, IntegrationProvider.WhatsApp, code, clock.UtcNow);

        linkCodes.Add(linkCode);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new WhatsAppLinkCodeResponse(code, linkCode.ExpiresAt));
    }
}
