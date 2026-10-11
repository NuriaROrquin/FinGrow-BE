namespace FinGrow.Api.Contracts;

using System.ComponentModel.DataAnnotations;

public sealed record ChangeDepartmentStatusRequest([Required] bool? IsActive, Guid? MoveEmployeesTo);
