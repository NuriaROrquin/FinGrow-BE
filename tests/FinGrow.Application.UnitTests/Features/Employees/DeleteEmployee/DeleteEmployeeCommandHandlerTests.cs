namespace FinGrow.Application.UnitTests.Features.Employees.DeleteEmployee;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Employees.DeleteEmployee;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class DeleteEmployeeCommandHandlerTests
{
    private readonly CompanyStaffScenario _scenario = new();

    private DeleteEmployeeCommandHandler CreateHandler() =>
        new(
            _scenario.CurrentUser,
            _scenario.Employees,
            _scenario.Receipts,
            _scenario.Notifications,
            _scenario.FileStorage,
            _scenario.UnitOfWork,
            NullLogger<DeleteEmployeeCommandHandler>.Instance);

    private TransactionReceipt AddReceipt(Employee employee)
    {
        var receipt = TransactionReceipt.Upload(employee.Id, "image/jpeg", 1024, "ticket.jpg", CompanyStaffScenario.CreatedAt);
        _scenario.Receipts.Add(receipt);
        _scenario.FileStorage.Files[receipt.StorageKey] = (new byte[] { 0xFF, 0xD8, 0xFF }, "image/jpeg");
        return receipt;
    }

    private void AddNotification(Guid employeeId) =>
        _scenario.Notifications.Add(Notification.Create(
            NotificationRecipient.Employee(employeeId),
            NotificationType.GoalStalled,
            "Tu meta está frenada",
            "Hace un mes que no le sumás",
            $"goal-stalled:{employeeId}",
            CompanyStaffScenario.CreatedAt));

    [Fact]
    public async Task Removes_the_employee_with_their_notifications_and_receipt_files()
    {
        var employee = _scenario.AddEmployee(department: null);
        var colleague = _scenario.AddEmployee(department: null);
        var receipt = AddReceipt(employee);
        var colleagueReceipt = AddReceipt(colleague);
        AddNotification(employee.Id);
        AddNotification(colleague.Id);

        var result = await CreateHandler().Handle(new DeleteEmployeeCommand(employee.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _scenario.Employees.Employees.ShouldNotContain(employee);
        _scenario.UnitOfWork.SaveCount.ShouldBe(1);
        _scenario.FileStorage.Files.ShouldNotContainKey(receipt.StorageKey);
        _scenario.FileStorage.Files.ShouldContainKey(colleagueReceipt.StorageKey);
        _scenario.Notifications.Notifications.ShouldHaveSingleItem()
            .IsAddressedTo(NotificationRecipient.Employee(colleague.Id)).ShouldBeTrue();
    }

    [Fact]
    public async Task An_unreachable_storage_does_not_undo_the_removal()
    {
        var employee = _scenario.AddEmployee(department: null);
        AddReceipt(employee);
        _scenario.FileStorage.Unreachable = true;

        var result = await CreateHandler().Handle(new DeleteEmployeeCommand(employee.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _scenario.Employees.Employees.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_employee_of_another_company_is_not_found_and_keeps_everything()
    {
        var other = CompanyStaffScenario.CreateCompany("Otra S.A.", "hola@otra.com");
        var foreign = _scenario.AddEmployee(department: null, company: other);
        AddNotification(foreign.Id);

        var result = await CreateHandler().Handle(new DeleteEmployeeCommand(foreign.Id), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        _scenario.Employees.Employees.ShouldContain(foreign);
        _scenario.Notifications.Notifications.Count.ShouldBe(1);
        _scenario.UnitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Requires_an_authenticated_company()
    {
        var employee = _scenario.AddEmployee(department: null);
        _scenario.LogOut();

        var result = await CreateHandler().Handle(new DeleteEmployeeCommand(employee.Id), CancellationToken.None);

        result.Error.Code.ShouldBe("Employee.NoAutenticado");
        _scenario.Employees.Employees.ShouldContain(employee);
    }
}
