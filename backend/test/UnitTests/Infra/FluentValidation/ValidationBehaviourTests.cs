using Castwork.Infra.FluentValidation.Behaviours;

using FluentValidation;

using MediatR;

namespace Castwork.UnitTests.Infra.FluentValidation;

public class ValidationBehaviourTests
{
    [Test]
    public async Task DeveChamarHandlerQuandoNaoHaValidatorsAsync()
    {
        var behaviour = new ValidationBehaviour<RequestDeTeste, string>([]);

        var resposta = await behaviour.Handle(new RequestDeTeste(string.Empty, 0), _ => Task.FromResult("ok"), CancellationToken.None);

        resposta.Should().Be("ok");
    }

    [Test]
    public async Task DeveLancarValidationExceptionComErrosDeTodosOsValidatorsSemChamarHandlerAsync()
    {
        var nome = new InlineValidator<RequestDeTeste>();
        nome.RuleFor(r => r.Nome).NotEmpty();
        var idade = new InlineValidator<RequestDeTeste>();
        idade.RuleFor(r => r.Idade).GreaterThan(0);
        var handlerChamado = false;
        var behaviour = new ValidationBehaviour<RequestDeTeste, string>([nome, idade]);
        RequestHandlerDelegate<string> handler = _ =>
        {
            handlerChamado = true;
            return Task.FromResult("ok");
        };

        var executar = () => behaviour.Handle(new RequestDeTeste(string.Empty, 0), handler, CancellationToken.None);

        (await executar.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Select(e => e.PropertyName).Should().BeEquivalentTo("nome", "idade");
        handlerChamado.Should().BeFalse();
    }

    public sealed record RequestDeTeste(string Nome, int Idade);
}
