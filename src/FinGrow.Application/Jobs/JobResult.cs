namespace FinGrow.Application.Jobs;

public sealed class JobResult
{
    private JobResult(bool isSuccess, string? summary, string? error)
    {
        IsSuccess = isSuccess;
        Summary = summary;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public string? Summary { get; }

    public string? Error { get; }

    public static JobResult Success(string? summary = null) => new(true, summary, null);

    public static JobResult Failure(string error, string? summary = null)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Un trabajo que falla tiene que decir por que.", nameof(error));
        }

        return new JobResult(false, summary, error);
    }
}
