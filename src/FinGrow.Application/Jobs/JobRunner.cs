namespace FinGrow.Application.Jobs;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal sealed partial class JobRunner
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IJobRunRepository _runs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly RunningJobs _running;
    private readonly ILogger<JobRunner> _logger;

    public JobRunner(
        IServiceScopeFactory scopes,
        IJobRunRepository runs,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        RunningJobs running,
        ILogger<JobRunner> logger)
    {
        _scopes = scopes;
        _runs = runs;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _running = running;
        _logger = logger;
    }

    public async Task<Result<JobRun>> RunAsync(string jobName, CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        var job = scope.ServiceProvider
            .GetServices<IScheduledJob>()
            .SingleOrDefault(candidate => string.Equals(candidate.Name, jobName, StringComparison.Ordinal));

        if (job is null)
        {
            return Result.Failure<JobRun>(JobErrors.NotFound(jobName));
        }

        if (!_running.TryStart(job.Name))
        {
            return Result.Failure<JobRun>(JobErrors.AlreadyRunning(job.Name));
        }

        try
        {
            var run = JobRun.Start(job.Name, _clock.UtcNow);
            _runs.Add(run);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            LogStarted(_logger, run.JobName, run.Id);

            await ExecuteAsync(job, run, cancellationToken);

            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            return Result.Success(run);
        }
        finally
        {
            _running.Finish(job.Name);
        }
    }

    private async Task ExecuteAsync(IScheduledJob job, JobRun run, CancellationToken cancellationToken)
    {
        try
        {
            var result = await job.ExecuteAsync(cancellationToken);

            if (result.IsSuccess)
            {
                run.Succeed(_clock.UtcNow, result.Summary);
                LogSucceeded(_logger, run.JobName, run.Duration ?? TimeSpan.Zero, run.Summary);
            }
            else
            {
                run.Fail(_clock.UtcNow, result.Error!, result.Summary);
                LogFailed(_logger, run.JobName, run.Error);
            }
        }
        catch (Exception exception)
        {
            LogCrashed(_logger, exception, run.JobName);
            run.Fail(_clock.UtcNow, Describe(exception, cancellationToken));
        }
    }

    private static string Describe(Exception exception, CancellationToken cancellationToken) =>
        exception is OperationCanceledException && cancellationToken.IsCancellationRequested
            ? "La corrida se cancelo antes de terminar: la API se estaba apagando o quien la disparo corto la conexion."
            : $"{exception.GetType().Name}: {exception.Message}";

    [LoggerMessage(Level = LogLevel.Information, Message = "Trabajo {JobName}: corrida {RunId} iniciada.")]
    private static partial void LogStarted(ILogger logger, string jobName, Guid runId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Trabajo {JobName}: corrida terminada en {Duration}. {Summary}")]
    private static partial void LogSucceeded(ILogger logger, string jobName, TimeSpan duration, string? summary);

    [LoggerMessage(Level = LogLevel.Error, Message = "Trabajo {JobName}: la corrida termino con error. {Error}")]
    private static partial void LogFailed(ILogger logger, string jobName, string? error);

    [LoggerMessage(Level = LogLevel.Error, Message = "Trabajo {JobName}: la corrida se corto por una excepcion no controlada.")]
    private static partial void LogCrashed(ILogger logger, Exception exception, string jobName);
}
