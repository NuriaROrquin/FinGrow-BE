namespace FinGrow.Infrastructure.Persistence.Configurations;

using System.Linq.Expressions;
using FinGrow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Un <see cref="Money"/> no tiene tabla propia: se guarda como dos columnas dentro de la tabla
/// de su entidad. En EF eso es un "owned type", el tipo que pertenece a otro y no existe suelto.
/// Esta extension centraliza como se llaman esas columnas y con que precision se guardan.
/// </summary>
internal static class MoneyConfigurationExtensions
{
    private const int TotalDigits = 18;

    public static void OwnsMoney<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, Money?>> navigation,
        string amountColumn,
        string currencyColumn)
        where TEntity : class
    {
        builder.OwnsOne(navigation, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName(amountColumn)
                .HasPrecision(TotalDigits, Money.DecimalPlaces)
                .IsRequired();

            money.Property(value => value.Currency)
                .HasColumnName(currencyColumn)
                .HasConversion<string>()
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Navigation(navigation).IsRequired();
    }
}
