namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class GoalContributionConfiguration : IEntityTypeConfiguration<GoalContribution>
{
    public void Configure(EntityTypeBuilder<GoalContribution> builder)
    {
        builder.ToTable("goal_contributions", table =>
            table.HasCheckConstraint("ck_goal_contributions_amount_positive", "amount > 0"));

        builder.HasKey(contribution => contribution.Id);

        builder.OwnsMoney(contribution => contribution.Amount, "amount", "currency");

        builder.Property(contribution => contribution.ContributedOn).IsRequired();

        builder.Property(contribution => contribution.Note)
            .HasMaxLength(GoalContribution.MaxNoteLength);

        builder.Property(contribution => contribution.CreatedAt).IsRequired();

        builder.HasIndex(contribution => new { contribution.GoalId, contribution.ContributedOn });
    }
}
