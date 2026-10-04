namespace FinGrow.Application.Features.Courses.ListCourses;

using Common;
using DTOs;
using Domain.Enums;
using MediatR;

public sealed record ListCoursesQuery(
    Guid EmployeeId,
    CourseLevel? Level,
    int? MaxDurationMinutes,
    CourseProgressStatus? ProgressStatus)
    : IRequest<Result<IReadOnlyList<CourseSummaryResponse>>>;
