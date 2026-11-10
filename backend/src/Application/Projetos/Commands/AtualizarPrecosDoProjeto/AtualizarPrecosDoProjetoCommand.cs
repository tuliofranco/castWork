using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Application.Precificacao;
using Castwork.Domain.Entities.Projetos;

using MediatR;

namespace Castwork.Application.Projetos.Commands.AtualizarPrecosDoProjeto;

/// <summary>
/// "Atualizar para os valores atuais": o projeto passa a ter as cotações vigentes e os parâmetros de hoje do ateliê,
/// e as edições feitas nele se perdem. A tela pede confirmação antes.
/// </summary>
[Permissao(Regras.Precificacao.Editar)]
public sealed class AtualizarPrecosDoProjetoCommand : ComandoDeProjeto, IRequest;

public sealed class AtualizarPrecosDoProjetoCommandHandler(IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<AtualizarPrecosDoProjetoCommand>
{
    public async Task Handle(AtualizarPrecosDoProjetoCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        projeto.AtualizarPrecosDoAtelie(await unitOfWork.ObterPrecosDoAtelieAsync(cancellationToken), timeProvider.GetUtcNow());
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}

public sealed class AtualizarPrecosDoProjetoCommandValidator : ValidatorBase<AtualizarPrecosDoProjetoCommand>
{
    public AtualizarPrecosDoProjetoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
        => this.ValidarProjeto(UnitOfWork);
}
