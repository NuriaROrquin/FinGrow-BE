namespace FinGrow.Domain.UnitTests;

using FinGrow.Domain.Errors;

public class DomainExceptionTests
{
    [Fact]
    public void A_DomainException_keeps_the_message_it_receives()
    {
        var exception = new DomainException("mensaje de prueba");

        exception.Message.ShouldBe("mensaje de prueba");
    }
}
