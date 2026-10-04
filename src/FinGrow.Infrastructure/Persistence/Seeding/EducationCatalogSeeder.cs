namespace FinGrow.Infrastructure.Persistence.Seeding;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed partial class EducationCatalogSeeder
{
    private static readonly DateTimeOffset SeedTimestamp = new(2025, 1, 2, 12, 0, 0, TimeSpan.Zero);

    private readonly FinGrowDbContext _dbContext;
    private readonly ILogger<EducationCatalogSeeder> _logger;

    public EducationCatalogSeeder(FinGrowDbContext dbContext, ILogger<EducationCatalogSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingCourses = await _dbContext.Courses
            .IgnoreAutoIncludes()
            .Select(course => course.Slug)
            .ToListAsync(cancellationToken);

        var existingArticles = await _dbContext.Articles
            .Select(article => article.Slug)
            .ToListAsync(cancellationToken);

        var courses = EducationCatalog.Courses
            .Where(seed => !existingCourses.Contains(seed.Slug))
            .Select(ToCourse)
            .ToList();

        var articles = EducationCatalog.Articles
            .Where(seed => !existingArticles.Contains(seed.Slug))
            .Select(ToArticle)
            .ToList();

        if (courses.Count == 0 && articles.Count == 0)
        {
            LogAlreadySeeded(_logger);
            return;
        }

        _dbContext.Courses.AddRange(courses);
        _dbContext.Articles.AddRange(articles);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var lessons = courses.Sum(course => course.Lessons.Count);
        LogSeeded(_logger, courses.Count, lessons, articles.Count);
    }

    internal static Course ToCourse(CourseSeed seed)
    {
        var course = Course.Create(
            seed.Slug,
            seed.Title,
            seed.Description,
            seed.Level,
            seed.Category,
            seed.RelatedInvestmentType,
            SeedTimestamp);

        foreach (var lesson in seed.Lessons)
        {
            course.AddLesson(lesson.Title, lesson.DurationMinutes, lesson.VideoUrl, SeedTimestamp);
        }

        course.Publish(SeedTimestamp);

        return course;
    }

    internal static Article ToArticle(ArticleSeed seed)
    {
        var article = Article.Create(
            seed.Slug,
            seed.Title,
            seed.Summary,
            seed.Content,
            seed.Category,
            seed.ReadingTimeMinutes,
            seed.RelatedInvestmentType,
            SeedTimestamp);

        article.Publish(SeedTimestamp);

        return article;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "El catalogo educativo ya estaba cargado; no se inserto nada.")]
    private static partial void LogAlreadySeeded(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Catalogo educativo cargado: {Courses} cursos con {Lessons} lecciones y {Articles} articulos.")]
    private static partial void LogSeeded(ILogger logger, int courses, int lessons, int articles);
}
