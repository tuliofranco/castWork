using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.ValueObjects;

using MediatR;

namespace Castwork.Application.Projetos.Commands.DefinirServicos;

/// <summary>
/// Serviços externos da versão (fundição, cravação, ródio, gravação): substitui a lista. Só em versão em desenvolvimento.
/// </summary>
[Permissao(Regras.Precificacao.Editar)]
public sealed class DefinirServicosCommand : ComandoDeVersao, IRequest
{
    public IReadOnlyList<ServicoInformado> Servicos { get; set; } = [];
}

public sealed class ServicoInformado
{
    public string Nome { get; set; } = string.Empty;

    public decimal Valor { get; set; }
}

public sealed class DefinirServicosCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DefinirServicosCommand>
{
    public async Task Handle(DefinirServicosCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        projeto.DefinirServicosDaVersao(request.VersaoId, request.Servicos.Select(s => new ServicoExterno(s.Nome, Dinheiro.Criar(s.Valor))));
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}
