namespace FinGrow.Application.DTOs;

using Domain.Entities;

public sealed record LessonResponse(
    Guid Id,
    int Position,
    string Title,
    int DurationMinutes,
    string VideoUrl,
    bool IsCompleted)
{
    public static LessonResponse FromEntity(Lesson lesson, bool isCompleted) => new(
        lesson.Id,
        lesson.Position,
        lesson.Title,
        lesson.DurationMinutes,
        lesson.VideoUrl,
        isCompleted);
}
