namespace FinGrow.Domain.UnitTests.Enums;

using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public class IncomeCategoryTests
{
    private static readonly string[] WireValuesInPython =
    {
        "salario",
        "freelance",
        "inversiones",
        "regalo",
        "otros"
    };

    [Fact]
    public void The_categories_match_one_to_one_with_the_ones_in_FinGrow_AI()
    {
        var wireValues = Enum.GetValues<IncomeCategory>()
            .Select(category => category.ToWireValue())
            .ToArray();

        wireValues.ShouldBe(WireValuesInPython);
    }

    [Theory]
    [InlineData("salario", IncomeCategory.Salario)]
    [InlineData("freelance", IncomeCategory.Freelance)]
    [InlineData("otros", IncomeCategory.Otros)]
    public void A_category_is_reconstructed_from_the_text_sent_by_the_AI(string wireValue, IncomeCategory expected)
    {
        IncomeCategoryExtensions.FromWireValue(wireValue).ShouldBe(expected);
    }

    [Fact]
    public void An_unknown_text_does_not_convert_into_a_category()
    {
        Should.Throw<DomainException>(() => IncomeCategoryExtensions.FromWireValue("aguinaldo"));
    }

    [Fact]
    public void TryFromWireValue_reports_failure_without_throwing_when_the_text_does_not_exist()
    {
        IncomeCategoryExtensions.TryFromWireValue("Salario", out _).ShouldBeFalse();
    }
}
