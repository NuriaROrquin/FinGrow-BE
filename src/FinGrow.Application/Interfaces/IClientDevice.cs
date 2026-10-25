namespace FinGrow.Application.Interfaces;

/// <summary>El dispositivo desde el que llega el pedido actual. Sin pedido HTTP (un trabajo, un
/// webhook) no hay dispositivo y todo vale null.</summary>
public interface IClientDevice
{
    string? UserAgent { get; }
}
