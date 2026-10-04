namespace FinGrow.Application.Features.Courses.CompleteLesson;

using Common;
using DTOs;
using MediatR;

public sealed record CompleteLessonCommand(Guid EmployeeId, string Slug, Guid LessonId)
    : IRequest<Result<CourseDetailResponse>>;
