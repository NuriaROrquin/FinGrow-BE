namespace FinGrow.Application.Features.Courses.RateCourse;

using Common;
using DTOs;
using MediatR;

public sealed record RateCourseCommand(Guid EmployeeId, string Slug, int Score) : IRequest<Result<CourseDetailResponse>>;
