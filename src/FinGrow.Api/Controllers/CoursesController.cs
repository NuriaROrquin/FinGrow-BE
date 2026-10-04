namespace FinGrow.Api.Controllers;

using Contracts;
using Extensions;
using Application.Common;
using Application.Features.Courses.CompleteLesson;
using Application.Features.Courses.GetCourse;
using Application.Features.Courses.ListCourses;
using Application.Features.Courses.RateCourse;
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

    [HttpGet("{slug}")]
    public async Task<IActionResult> Get(string slug, CancellationToken cancellationToken) =>
        (await sender.Send(new GetCourseQuery(currentUser.UserId!.Value, slug), cancellationToken)).ToActionResult();

    [HttpPut("{slug}/lessons/{lessonId:guid}/completion")]
    public async Task<IActionResult> CompleteLesson(string slug, Guid lessonId, CancellationToken cancellationToken) =>
        (await sender.Send(
            new CompleteLessonCommand(currentUser.UserId!.Value, slug, lessonId),
            cancellationToken)).ToActionResult();

    [HttpPut("{slug}/rating")]
    public async Task<IActionResult> Rate(string slug, RateCourseRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(
            new RateCourseCommand(currentUser.UserId!.Value, slug, request.Score),
            cancellationToken)).ToActionResult();
}
