namespace FinGrow.Domain.UnitTests.Enums;

using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

/// <summary>
/// Este archivo es el contrato con FinGrow-AI. Si alguien agrega o renombra una categoria de un
/// lado y no del otro, la IA va a devolver un valor que el backend no sabe guardar. El test
/// existe para que esa desincronizacion rompa el build y no la produccion.
/// La fuente de verdad del otro lado es FinGrow-AI/app/domain/enums.py.
/// </summary>
public class ExpenseCategoryTests
{
    private static readonly string[] WireValuesInPython =
    [
        "alimentos",
        "transporte",
        "vivienda",
        "servicios",
        "salud",
        "educacion",
        "entretenimiento",
        "indumentaria",
        "ahorro_inversion",
        "otros"
    ];

    [Fact]
    public void There_are_exactly_ten_expense_categories()
    {
        Enum.GetValues<ExpenseCategory>().Length.ShouldBe(10);
    }

    [Fact]
    public void The_categories_match_one_to_one_with_the_ones_in_FinGrow_AI()
    {
        var wireValues = Enum.GetValues<ExpenseCategory>()
            .Select(category => category.ToWireValue())
            .ToArray();

        wireValues.ShouldBe(WireValuesInPython);
    }

    [Theory]
    [InlineData("alimentos", ExpenseCategory.Alimentos)]
    [InlineData("ahorro_inversion", ExpenseCategory.AhorroInversion)]
    [InlineData("otros", ExpenseCategory.Otros)]
    public void A_category_is_reconstructed_from_the_text_sent_by_the_AI(string wireValue, ExpenseCategory expected)
    {
        ExpenseCategoryExtensions.FromWireValue(wireValue).ShouldBe(expected);
    }

    [Fact]
    public void An_unknown_text_does_not_convert_into_a_category()
    {
        Should.Throw<DomainException>(() => ExpenseCategoryExtensions.FromWireValue("criptomonedas"));
    }

    [Fact]
    public void TryFromWireValue_reports_failure_without_throwing_when_the_text_does_not_exist()
    {
        ExpenseCategoryExtensions.TryFromWireValue("Alimentos", out _).ShouldBeFalse();
    }
}
