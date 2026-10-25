namespace FinGrow.Infrastructure.Persistence.Repositories;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

internal static class SortExpressions
{
    public const string SpanishCollation = "es-x-icu";

    public static Expression<Func<TEntity, int>> RankBy<TEntity>(
        params Expression<Func<TEntity, bool>>[] conditionsInOrder)
    {
        var entity = Expression.Parameter(typeof(TEntity), "entity");
        Expression rank = Expression.Constant(conditionsInOrder.Length);

        for (var index = conditionsInOrder.Length - 1; index >= 0; index--)
        {
            var condition = conditionsInOrder[index];
            var body = ReplacingExpressionVisitor.Replace(condition.Parameters[0], entity, condition.Body);
            rank = Expression.Condition(body, Expression.Constant(index), rank);
        }

        return Expression.Lambda<Func<TEntity, int>>(rank, entity);
    }

    public static IOrderedQueryable<TEntity> OrderByDirection<TEntity, TKey>(
        this IQueryable<TEntity> query,
        Expression<Func<TEntity, TKey>> keySelector,
        bool ascending) =>
        ascending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
}
