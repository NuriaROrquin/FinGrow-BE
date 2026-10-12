namespace FinGrow.Application.UnitTests.Authorization;

using FinGrow.Application.Common;

public class OwnershipTests
{
    private static readonly Error NotFound = Error.NotFound("Fake.NotFound", "missing");
    private static readonly Error NotOwned = Error.Forbidden("Fake.NotOwned", "not yours");

    private sealed record FakeResource(Guid EmployeeId);

    [Fact]
    public void A_missing_resource_is_not_found()
    {
        var result = Ownership.Require<FakeResource>(null, r => r.EmployeeId, Guid.NewGuid(), NotFound, NotOwned);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(NotFound);
    }

    [Fact]
    public void A_resource_of_another_employee_is_forbidden()
    {
        var resource = new FakeResource(Guid.NewGuid());

        var result = Ownership.Require(resource, r => r.EmployeeId, Guid.NewGuid(), NotFound, NotOwned);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(NotOwned);
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
    }

    [Fact]
    public void An_own_resource_is_returned()
    {
        var employeeId = Guid.NewGuid();
        var resource = new FakeResource(employeeId);

        var result = Ownership.Require(resource, r => r.EmployeeId, employeeId, NotFound, NotOwned);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(resource);
    }
}
