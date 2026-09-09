namespace FinGrow.Application.UnitTests;

using FinGrow.Application.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

public sealed record FakeCommand(string Nombre) : IRequest<Result<string>>;

public sealed class FakeCommandValidator : AbstractValidator<FakeCommand>
{
    public FakeCommandValidator() => RuleFor(command => command.Nombre).NotEmpty();
}

public sealed class FakeCommandHandler : IRequestHandler<FakeCommand, Result<string>>
{
    public Task<Result<string>> Handle(FakeCommand request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success($"hola {request.Nombre}"));
}

public class DependencyInjectionTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        var testAssembly = typeof(DependencyInjectionTests).Assembly;

        services.AddApplication();
        services.AddValidatorsFromAssembly(testAssembly);
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(testAssembly));

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task AddApplication_resuelve_un_handler_nuevo_sin_registro_manual_adicional()
    {
        await using var provider = BuildProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new FakeCommand("Cande"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("hola Cande");
    }

    [Fact]
    public async Task El_pipeline_de_validacion_corta_antes_de_llegar_al_handler()
    {
        await using var provider = BuildProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new FakeCommand(string.Empty));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }
}
