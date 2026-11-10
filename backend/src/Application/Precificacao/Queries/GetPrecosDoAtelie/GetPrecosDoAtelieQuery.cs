using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Acesso;
using Castwork.Domain.Entities.Atelie;
using Castwork.Domain.Entities.Precificacao;

using MediatR;

namespace Castwork.Application.Precificacao.Queries.GetPrecosDoAtelie;

[Permissao(Regras.Precificacao.Consultar)]
public sealed class GetPrecosDoAtelieQuery : IRequest<PrecosDoAtelieDto>;

public sealed class GetPrecosDoAtelieQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetPrecosDoAtelieQuery, PrecosDoAtelieDto>
{
    public async Task<PrecosDoAtelieDto> Handle(GetPrecosDoAtelieQuery request, CancellationToken cancellationToken)
    {
        var usuarios = unitOfWork.GetRepository<Usuario>().FindAll();
        var cotacoes = new List<CotacaoVigenteDto>();
        foreach (var metal in Enum.GetValues<Metal>())
        {
            var atual = await unitOfWork.GetRepository<CotacaoMetal>()
                .FindBy(c => c.Metal == metal)
                .MaisRecentesPrimeiro()
                .ParaDto(usuarios)
                .FirstOrDefaultAsync(cancellationToken);
            cotacoes.Add(new CotacaoVigenteDto { Metal = metal, Atual = atual });
        }

        var ligas = await unitOfWork.GetRepository<Liga>()
            .FindBy(l => l.Ativo)
            .OrderBy(l => l.Metal).ThenByDescending(l => l.Teor).ThenBy(l => l.Nome)
            .Select(l => new LigaDto { Id = l.Id, Nome = l.Nome, Metal = l.Metal, Teor = l.Teor.Milesimos, Densidade = l.Densidade })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var parametros = await unitOfWork.GetRepository<ConfiguracaoAtelie>()
            .FindBy(c => c.Id == ConfiguracaoAtelie.IdUnico)
            .Select(c => new ParametrosDePrecoDto
            {
                PerdaPercentual = c.ParametrosDePreco.PerdaPercentual,
                ValorHora = c.ParametrosDePreco.ValorHora.Valor,
                Markup = c.ParametrosDePreco.Markup,
                TaxasPercentual = c.ParametrosDePreco.TaxasPercentual,
                ArredondarPara = c.ParametrosDePreco.ArredondarPara.Valor,
            })
            .AsNoTracking()
            .SingleAsync(cancellationToken);

        return new PrecosDoAtelieDto { Cotacoes = cotacoes, Ligas = ligas, Parametros = parametros };
    }
}
