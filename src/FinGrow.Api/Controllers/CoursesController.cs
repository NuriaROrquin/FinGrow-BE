namespace FinGrow.Api.Controllers;

using Extensions;
using Application.Common;
using Application.Features.Courses.ListCourses;
using Application.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/courses")]
[Authorize(Roles = Rol.Empleado)]
public sealed class CoursesController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] CourseLevel? level,
        [FromQuery] int? maxDuration,
        [FromQuery] CourseProgressStatus? status,
        CancellationToken cancellationToken) =>
        (await sender.Send(
            new ListCoursesQuery(currentUser.UserId!.Value, level, maxDuration, status),
            cancellationToken)).ToActionResult();
}
