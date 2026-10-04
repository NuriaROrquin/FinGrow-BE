namespace FinGrow.Application.Features.Courses.GetCourse;

using Common;
using DTOs;
using MediatR;

public sealed record GetCourseQuery(Guid EmployeeId, string Slug) : IRequest<Result<CourseDetailResponse>>;
