namespace FinGrow.Application.Features.Courses.GetLearningProgress;

using Common;
using DTOs;
using MediatR;

public sealed record GetLearningProgressQuery(Guid EmployeeId) : IRequest<Result<LearningProgressResponse>>;
