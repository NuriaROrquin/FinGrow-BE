namespace FinGrow.Application.Jobs;

using FinGrow.Application.Common;

public static class JobErrors
{
    public static Error NotFound(string jobName) =>
        Error.NotFound("Jobs.NotFound", $"No existe un trabajo llamado '{jobName}'.");

    public static Error AlreadyRunning(string jobName) =>
        Error.Conflict("Jobs.AlreadyRunning", $"El trabajo '{jobName}' ya esta corriendo; esperá a que termine.");
}
