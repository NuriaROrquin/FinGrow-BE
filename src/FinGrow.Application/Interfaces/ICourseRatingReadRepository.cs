namespace FinGrow.Application.Interfaces;

public interface ICourseRatingReadRepository
{
    Task<IReadOnlyDictionary<Guid, CourseRatingSummary>> GetSummariesAsync(
        Guid employeeId,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default);
}

public sealed record CourseRatingSummary(int Count, decimal? Average, int? EmployeeScore)
{
    public static readonly CourseRatingSummary None = new(0, null, null);
}
