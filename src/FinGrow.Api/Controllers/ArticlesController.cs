namespace FinGrow.Api.Controllers;

using Extensions;
using Application.Common;
using Application.Features.Articles.GetArticle;
using Application.Features.Articles.ListArticles;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/articles")]
[Authorize(Roles = Rol.Empleado)]
public sealed class ArticlesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] EducationCategory? category,
        [FromQuery] int? maxReadingTime,
        CancellationToken cancellationToken) =>
        (await sender.Send(new ListArticlesQuery(category, maxReadingTime), cancellationToken)).ToActionResult();

    [HttpGet("{slug}")]
    public async Task<IActionResult> Get(string slug, CancellationToken cancellationToken) =>
        (await sender.Send(new GetArticleQuery(slug), cancellationToken)).ToActionResult();
}
