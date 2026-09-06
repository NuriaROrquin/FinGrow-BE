namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions", table =>
        {
            // El dominio ya impide construir un gasto con categoria de ingreso. Esta restriccion
            // repite la regla en la base para que tampoco pueda entrar por una carga masiva,
            // una migracion de datos o un UPDATE hecho a mano.
            table.HasCheckConstraint(
                "ck_transactions_category_matches_type",
                """
                (type = 'Expense' AND expense_category IS NOT NULL AND income_category IS NULL)
                OR (type = 'Income' AND income_category IS NOT NULL AND expense_category IS NULL)
                """);

            table.HasCheckConstraint("ck_transactions_amount_positive", "amount > 0");
        });

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.OwnsMoney(transaction => transaction.Amount, "amount", "currency");

        builder.Property(transaction => transaction.ExpenseCategory)
            .HasConversion(new ExpenseCategoryConverter())
            .HasMaxLength(ExpenseCategoryConverter.MaxLength);

        builder.Property(transaction => transaction.IncomeCategory)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(transaction => transaction.Description)
            .HasMaxLength(Transaction.MaxDescriptionLength)
            .IsRequired();

        builder.Property(transaction => transaction.OccurredOn).IsRequired();

        builder.Property(transaction => transaction.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(transaction => transaction.Source)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(transaction => transaction.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Ignore(transaction => transaction.IsPending);

        builder.Property(transaction => transaction.ExternalReference)
            .HasMaxLength(Transaction.MaxExternalReferenceLength);

        builder.Property(transaction => transaction.CreatedAt).IsRequired();
        builder.Property(transaction => transaction.UpdatedAt).IsRequired();

        builder.HasOne(transaction => transaction.Employee)
            .WithMany()
            .HasForeignKey(transaction => transaction.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // El listado y los graficos siempre filtran por empleado y ordenan por fecha (HU-18, HU-19).
        builder.HasIndex(transaction => new { transaction.EmployeeId, transaction.OccurredOn });

        // La bandeja de "esto propuso la IA, revisalo" busca los pendientes de un empleado (HU-15).
        builder.HasIndex(transaction => new { transaction.EmployeeId, transaction.Status });

        // Evita que una re-sincronizacion de Gmail o Mercado Pago cargue dos veces el mismo
        // movimiento. Solo aplica a lo que vino de afuera: las cargas manuales no tienen referencia.
        builder.HasIndex(transaction => new
            {
                transaction.EmployeeId,
                transaction.Source,
                transaction.ExternalReference
            })
            .IsUnique()
            .HasFilter("external_reference IS NOT NULL");
    }
}
