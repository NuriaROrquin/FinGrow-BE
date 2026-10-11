namespace FinGrow.Api.Contracts;

using System.ComponentModel.DataAnnotations;

public sealed record ChangeEmployeeAccessRequest([Required] bool? IsActive);
