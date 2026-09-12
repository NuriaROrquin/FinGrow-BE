namespace FinGrow.Application.UnitTests;

using FinGrow.Application.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

public sealed record FakeCommand(string Name) : IRequest<Result<string>>;

public sealed class FakeCommandValidator : AbstractValidator<FakeCommand>
{
    public FakeCommandValidator() => RuleFor(command => command.Name).NotEmpty();
}

public sealed class FakeCommandHandler : IRequestHandler<FakeCommand, Result<string>>
{
    public Task<Result<string>> Handle(FakeCommand request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success($"hello {request.Name}"));
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
    public async Task AddApplication_resolves_a_new_handler_without_additional_manual_registration()
    {
        await using var provider = BuildProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new FakeCommand("Cande"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("hello Cande");
    }

    [Fact]
    public async Task The_validation_pipeline_short_circuits_before_reaching_the_handler()
    {
        await using var provider = BuildProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new FakeCommand(string.Empty));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }
}
