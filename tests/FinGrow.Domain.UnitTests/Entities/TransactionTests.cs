namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class TransactionTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 3, 15);

    [Fact]
    public void Un_gasto_queda_con_categoria_de_gasto_y_sin_categoria_de_ingreso()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));

        transaction.Type.ShouldBe(TransactionType.Expense);
        transaction.ExpenseCategory.ShouldBe(ExpenseCategory.Servicios);
        transaction.IncomeCategory.ShouldBeNull();
    }

    [Fact]
    public void Un_ingreso_queda_con_categoria_de_ingreso_y_sin_categoria_de_gasto()
    {
        var transaction = Transaction.RegisterIncome(
            EmployeeId,
            Money.From(850000m, Currency.ARS),
            IncomeCategory.Salario,
            "Sueldo de marzo",
            Today,
            PaymentMethod.BankTransfer,
            TransactionSource.Manual,
            TransactionStatus.Confirmed,
            Now);

        transaction.Type.ShouldBe(TransactionType.Income);
        transaction.IncomeCategory.ShouldBe(IncomeCategory.Salario);
        transaction.ExpenseCategory.ShouldBeNull();
    }

    [Fact]
    public void Un_movimiento_de_importe_cero_no_se_registra()
    {
        Should.Throw<DomainException>(() => RegisterExpense(Money.Zero(Currency.ARS)));
    }

    [Fact]
    public void Un_movimiento_sin_descripcion_no_se_registra()
    {
        Should.Throw<DomainException>(() => Transaction.RegisterExpense(
            EmployeeId,
            Money.From(100m, Currency.ARS),
            ExpenseCategory.Otros,
            "   ",
            Today,
            PaymentMethod.Cash,
            TransactionSource.Manual,
            TransactionStatus.Confirmed,
            Now));
    }

    [Fact]
    public void Un_movimiento_siempre_pertenece_a_un_empleado()
    {
        Should.Throw<DomainException>(() => Transaction.RegisterExpense(
            Guid.Empty,
            Money.From(100m, Currency.ARS),
            ExpenseCategory.Otros,
            "Cafe",
            Today,
            PaymentMethod.Cash,
            TransactionSource.Manual,
            TransactionStatus.Confirmed,
            Now));
    }

    [Fact]
    public void Un_movimiento_que_carga_el_empleado_a_mano_nace_confirmado()
    {
        var transaction = Transaction.RegisterIncome(
            EmployeeId,
            Money.From(850000m, Currency.ARS),
            IncomeCategory.Salario,
            "Sueldo de marzo",
            Today,
            PaymentMethod.BankTransfer,
            TransactionSource.Manual,
            TransactionStatus.Confirmed,
            Now);

        transaction.Status.ShouldBe(TransactionStatus.Confirmed);
        transaction.IsPending.ShouldBeFalse();
    }

    [Fact]
    public void Un_movimiento_que_propone_una_integracion_nace_pendiente()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));

        transaction.Status.ShouldBe(TransactionStatus.Pending);
        transaction.IsPending.ShouldBeTrue();
    }

    [Fact]
    public void Confirmar_una_propuesta_la_deja_lista_para_contar()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));
        var confirmedAt = Now.AddMinutes(10);

        transaction.Confirm(confirmedAt);

        transaction.Status.ShouldBe(TransactionStatus.Confirmed);
        transaction.UpdatedAt.ShouldBe(confirmedAt);
    }

    [Fact]
    public void Un_movimiento_ya_confirmado_no_se_vuelve_a_confirmar()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));
        transaction.Confirm(Now);

        Should.Throw<DomainException>(() => transaction.Confirm(Now.AddMinutes(1)));
    }

    [Fact]
    public void Un_gasto_se_puede_recategorizar_cuando_el_empleado_corrige_a_la_IA()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));

        transaction.RecategorizeExpense(ExpenseCategory.Vivienda, Now.AddMinutes(5));

        transaction.ExpenseCategory.ShouldBe(ExpenseCategory.Vivienda);
        transaction.UpdatedAt.ShouldBe(Now.AddMinutes(5));
    }

    [Fact]
    public void Un_gasto_no_acepta_una_categoria_de_ingreso()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));

        Should.Throw<DomainException>(() => transaction.RecategorizeIncome(IncomeCategory.Salario, Now));
    }

    private static Transaction RegisterExpense(Money amount) => Transaction.RegisterExpense(
        EmployeeId,
        amount,
        ExpenseCategory.Servicios,
        "Factura de luz",
        Today,
        PaymentMethod.DebitCard,
        TransactionSource.Gmail,
        TransactionStatus.Pending,
        Now,
        externalReference: "gmail-message-123");
}
