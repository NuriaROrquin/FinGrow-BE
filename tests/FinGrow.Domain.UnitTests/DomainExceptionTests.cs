namespace FinGrow.Domain.UnitTests;

using FinGrow.Domain.Errors;
using FluentAssertions;

public class DomainExceptionTests
{
    [Fact]
    public void Una_DomainException_conserva_el_mensaje_que_recibe()
    {
        var exception = new DomainException("mensaje de prueba");

        exception.Message.Should().Be("mensaje de prueba");
    }
}
