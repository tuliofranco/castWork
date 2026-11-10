using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.ValueObjects;

using MediatR;

namespace Castwork.Application.Projetos.Commands.DefinirMaoDeObra;

/// <summary>
/// Mão de obra da versão: por horas (× valor da hora do projeto) ou valor fixo. Só em versão em desenvolvimento.
/// </summary>
[Permissao(Regras.Precificacao.Editar)]
public sealed class DefinirMaoDeObraCommand : ComandoDeVersao, IRequest
{
    public TipoMaoDeObra Tipo { get; set; }

    /// <summary>
    /// Horas de bancada, quando é por horas.
    /// </summary>
    public decimal? Horas { get; set; }

    /// <summary>
    /// Valor combinado (R$), quando é valor fixo.
    /// </summary>
    public decimal? Valor { get; set; }
}

public sealed class DefinirMaoDeObraCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DefinirMaoDeObraCommand>
{
    public async Task Handle(DefinirMaoDeObraCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        var maoDeObra = request.Tipo == TipoMaoDeObra.PorHoras
            ? MaoDeObra.PorHoras(request.Horas ?? 0m)
            : MaoDeObra.Fixa(Dinheiro.Criar(request.Valor ?? 0m));
        projeto.DefinirMaoDeObraDaVersao(request.VersaoId, maoDeObra);
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}
