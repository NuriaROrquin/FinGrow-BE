namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Errors;

public sealed class CourseRating : AggregateRoot
{
    public const int MinScore = 1;
    public const int MaxScore = 5;

    private CourseRating()
    {
    }

    private CourseRating(Guid id, Guid employeeId, Guid courseId, int score, DateTimeOffset ratedAt)
        : base(id)
    {
        EmployeeId = employeeId;
        CourseId = courseId;
        Score = score;
        RatedAt = ratedAt;
        UpdatedAt = ratedAt;
    }

    public Guid EmployeeId { get; private set; }

    public Guid CourseId { get; private set; }

    public int Score { get; private set; }

    public DateTimeOffset RatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static CourseRating Create(Guid employeeId, Guid courseId, int score, DateTimeOffset ratedAt) =>
        new(Guid.CreateVersion7(), employeeId, courseId, EnsureValidScore(score), ratedAt);

    public void ChangeScore(int score, DateTimeOffset updatedAt)
    {
        Score = EnsureValidScore(score);
        UpdatedAt = updatedAt;
    }

    private static int EnsureValidScore(int score) =>
        score is < MinScore or > MaxScore
            ? throw new DomainException($"La calificacion tiene que estar entre {MinScore} y {MaxScore}.")
            : score;
}
