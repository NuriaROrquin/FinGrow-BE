namespace FinGrow.Application.Features.Courses.ListCourses;

using Common;
using DTOs;
using Domain.Enums;
using MediatR;

/// <param name="MaxDurationMinutes">Solo los cursos que duran ese tiempo o menos.</param>
/// <param name="ProgressStatus">Solo los cursos en los que el empleado esta en ese punto.</param>
public sealed record ListCoursesQuery(
    Guid EmployeeId,
    CourseLevel? Level,
    int? MaxDurationMinutes,
    CourseProgressStatus? ProgressStatus)
    : IRequest<Result<IReadOnlyList<CourseSummaryResponse>>>;
