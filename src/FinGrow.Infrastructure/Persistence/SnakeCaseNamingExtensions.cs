namespace FinGrow.Infrastructure.Persistence;

using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

/// <summary>
/// PostgreSQL pasa a minusculas cualquier identificador que no venga entre comillas. Si dejamos
/// los nombres que genera EF por convencion (PascalCase), toda consulta escrita a mano en psql
/// necesita comillas dobles. Este recorrido reescribe tablas, columnas, claves e indices a
/// snake_case una sola vez, en lugar de repetir HasColumnName en cada propiedad.
/// </summary>
internal static class SnakeCaseNamingExtensions
{
    public static void UseSnakeCaseNames(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            RenameTable(entityType);
            RenameColumns(entityType);

            foreach (var key in entityType.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName()));
            }

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName()));
            }

            foreach (var index in entityType.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()));
            }
        }
    }

    private static void RenameTable(IMutableEntityType entityType)
    {
        // Lo que ya trae un nombre explicito desde su IEntityTypeConfiguration se respeta.
        if (entityType.FindAnnotation(RelationalAnnotationNames.TableName) is not null)
        {
            return;
        }

        if (entityType.GetTableName() is { } tableName)
        {
            entityType.SetTableName(ToSnakeCase(tableName));
        }
    }

    private static void RenameColumns(IMutableEntityType entityType)
    {
        var table = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table);

        foreach (var property in entityType.GetProperties())
        {
            if (property.FindAnnotation(RelationalAnnotationNames.ColumnName) is not null)
            {
                continue;
            }

            var columnName = table is null ? property.Name : property.GetColumnName(table.Value);
            property.SetColumnName(ToSnakeCase(columnName));
        }
    }

    private static string? ToSnakeCase(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new StringBuilder(name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];

            if (current == '_')
            {
                builder.Append('_');
                continue;
            }

            if (char.IsUpper(current) && i > 0 && NeedsSeparator(name, i))
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }

    // "CompanyId" -> company_id, pero "TaxId" tampoco se corta en "ta_x_id":
    // solo se separa cuando la mayuscula arranca una palabra nueva.
    private static bool NeedsSeparator(string name, int index) =>
        name[index - 1] != '_'
        && (!char.IsUpper(name[index - 1]) || (index + 1 < name.Length && char.IsLower(name[index + 1])));
}
