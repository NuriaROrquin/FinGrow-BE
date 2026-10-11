namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Errors;

public sealed class EmployeeDepartmentAssignment : Entity
{
    private EmployeeDepartmentAssignment()
    {
    }

    private EmployeeDepartmentAssignment(Guid id, Guid employeeId, Guid departmentId, DateTimeOffset startedAt)
        : base(id)
    {
        EmployeeId = employeeId;
        DepartmentId = departmentId;
        StartedAt = startedAt;
    }

    public Guid EmployeeId { get; private set; }

    public Guid DepartmentId { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? EndedAt { get; private set; }

    public bool IsCurrent => EndedAt is null;

    internal static EmployeeDepartmentAssignment Start(Guid employeeId, Guid departmentId, DateTimeOffset startedAt) =>
        departmentId == Guid.Empty
            ? throw new DomainException("Una asignacion siempre es a un departamento.")
            : new EmployeeDepartmentAssignment(Guid.CreateVersion7(), employeeId, departmentId, startedAt);

    internal void End(DateTimeOffset endedAt)
    {
        if (endedAt < StartedAt)
        {
            throw new DomainException("Una asignacion no puede terminar antes de empezar.");
        }

        EndedAt = endedAt;
    }
}
