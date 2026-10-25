namespace FinGrow.ArchitectureTests;

using System.Reflection;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;

/// <summary>
/// La empresa nunca ve datos de una persona identificable: lo que leen sus métricas y lo que se le
/// manda a FinGrow-AI sobre ella no puede incorporar un campo que identifique a un empleado.
/// </summary>
public class AnonymizedContractRules
{
    private const string MetricsNamespace = "FinGrow.Application.Features.Metrics";

    private static readonly string[] PersonalFields =
    {
        "EmployeeId",
        "UserId",
        "Email",
        "FullName",
        "EmployeeName",
        "FirstName",
        "LastName",
        "TransactionId",
        "GoalId",
        "InvestmentId",
    };

    [Fact]
    public void Company_metrics_responses_do_not_expose_personal_identifiers()
    {
        var responses = AssemblyReference.Application.GetTypes()
            .Where(type => type.IsPublic
                && type.Name.EndsWith("Response", StringComparison.Ordinal)
                && type.Namespace?.StartsWith(MetricsNamespace, StringComparison.Ordinal) == true)
            .Append(typeof(CompanyParticipationResponse))
            .ToList();

        responses.Count.ShouldBeGreaterThan(1);

        var violations = responses
            .SelectMany(Graph)
            .Distinct()
            .SelectMany(type => PublicProperties(type)
                .Where(property => PersonalFields.Contains(property.Name, StringComparer.Ordinal))
                .Select(property => $"{type.Name}.{property.Name}"))
            .ToList();

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void The_company_recommendations_input_only_carries_aggregates()
    {
        var violations = Graph(typeof(CompanyRecommendationsInput))
            .SelectMany(type => PublicProperties(type)
                .Where(property => Leaves(property.PropertyType)
                    .Any(leaf => leaf == typeof(Guid) || leaf == typeof(string)))
                .Select(property => $"{type.Name}.{property.Name}"))
            .ToList();

        violations.ShouldBeEmpty();
    }

    private static PropertyInfo[] PublicProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

    private static HashSet<Type> Graph(Type root)
    {
        var seen = new HashSet<Type>();
        var pending = new Queue<Type>();
        pending.Enqueue(root);

        while (pending.TryDequeue(out var type))
        {
            if (!seen.Add(type))
            {
                continue;
            }

            foreach (var leaf in PublicProperties(type).SelectMany(property => Leaves(property.PropertyType)))
            {
                if (!leaf.IsEnum && leaf.Namespace?.StartsWith("FinGrow.", StringComparison.Ordinal) == true)
                {
                    pending.Enqueue(leaf);
                }
            }
        }

        return seen;
    }

    private static IEnumerable<Type> Leaves(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return Leaves(underlying);
        }

        if (type.IsArray)
        {
            return Leaves(type.GetElementType()!);
        }

        return type.IsGenericType
            ? type.GetGenericArguments().SelectMany(Leaves)
            : new[] { type };
    }
}
