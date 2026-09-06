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
    public void Hay_exactamente_diez_categorias_de_gasto()
    {
        Enum.GetValues<ExpenseCategory>().Length.ShouldBe(10);
    }

    [Fact]
    public void Las_categorias_coinciden_una_a_una_con_las_de_FinGrow_AI()
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
    public void Una_categoria_se_reconstruye_desde_el_texto_que_manda_la_IA(string wireValue, ExpenseCategory expected)
    {
        ExpenseCategoryExtensions.FromWireValue(wireValue).ShouldBe(expected);
    }

    [Fact]
    public void Un_texto_desconocido_no_se_convierte_en_categoria()
    {
        Should.Throw<DomainException>(() => ExpenseCategoryExtensions.FromWireValue("criptomonedas"));
    }

    [Fact]
    public void TryFromWireValue_avisa_sin_lanzar_cuando_el_texto_no_existe()
    {
        ExpenseCategoryExtensions.TryFromWireValue("Alimentos", out _).ShouldBeFalse();
    }
}
