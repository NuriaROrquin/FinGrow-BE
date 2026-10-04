namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Common;
using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("articles", table =>
            table.HasCheckConstraint("ck_articles_reading_time_positive", "reading_time_minutes > 0"));

        builder.HasKey(article => article.Id);

        builder.Property(article => article.Slug)
            .HasMaxLength(Slug.MaxLength)
            .IsRequired();

        builder.Property(article => article.Title)
            .HasMaxLength(Article.MaxTitleLength)
            .IsRequired();

        builder.Property(article => article.Summary)
            .HasMaxLength(Article.MaxSummaryLength)
            .IsRequired();

        builder.Property(article => article.Content)
            .HasMaxLength(Article.MaxContentLength)
            .IsRequired();

        builder.Property(article => article.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(article => article.ReadingTimeMinutes).IsRequired();

        builder.Property(article => article.RelatedInvestmentType)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(article => article.CreatedAt).IsRequired();
        builder.Property(article => article.UpdatedAt).IsRequired();

        builder.Ignore(article => article.IsPublished);

        builder.HasIndex(article => article.Slug).IsUnique();
        builder.HasIndex(article => article.Category);
        builder.HasIndex(article => article.RelatedInvestmentType);
    }
}
