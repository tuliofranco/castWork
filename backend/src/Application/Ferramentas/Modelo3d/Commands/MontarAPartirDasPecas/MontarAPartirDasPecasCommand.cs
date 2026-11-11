using Castwork.Application.Abstractions;
using Castwork.Application.Assistente;
using Castwork.Application.Common.Security;
using Castwork.Application.Projetos.Commands;
using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;

using MediatR;

namespace Castwork.Application.Ferramentas.Modelo3d.Commands.MontarAPartirDasPecas;

/// <summary>
/// "Montar a partir das peças": o modelo 3D da versão recebe uma cópia do modelo 3D de cada peça da composição, uma por
/// unidade (a quantidade da composição), enfileiradas ao lado do que já está no modelo, para a IA ou o joalheiro
/// posicionarem depois. Vale o arquivo final do modelo 3D da peça; cada unidade leva o nome da peça e o número dela
/// ("Elo oval (1)"). Os corpos mantêm o material da peça (os sem material recebem a liga da peça). Peça sem modelo 3D final
/// fica de fora (e é listada). Grava uma nova versão do arquivo (desfazível pelo histórico). Só em versão em
/// desenvolvimento e sem a IA trabalhando no modelo.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class MontarAPartirDasPecasCommand : ComandoDeVersao, IRequest<ModeloMontadoDto>
{
    /// <summary>
    /// Quantas unidades, no máximo, entram numa montagem (cada uma é uma cópia do modelo da peça).
    /// </summary>
    public const int UnidadesMaximo = 100;
}

public sealed class MontarAPartirDasPecasCommandHandler(IUnitOfWork unitOfWork, IMontadorDoModelo montador, TimeProvider relogio)
    : IRequestHandler<MontarAPartirDasPecasCommand, ModeloMontadoDto>
{
    public async Task<ModeloMontadoDto> Handle(MontarAPartirDasPecasCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        projeto.GarantirVersaoEmDesenvolvimento(request.VersaoId);
        var versao = projeto.Versoes.Single(v => v.Id == request.VersaoId);

        var agora = relogio.GetUtcNow();
        var escopo = EscopoDaFerramenta.DaVersao(request.ProjetoId, request.VersaoId);
        var conversa = await ConversaDoEscopo.ObterAsync(unitOfWork, escopo, Modelo3dNoAssistente.Codigo, cancellationToken);
        if (conversa?.AguardandoIa(agora) == true)
        {
            throw new RegraDeNegocioException("A IA está editando o modelo. Espere ela terminar para montar a partir das peças.");
        }

        var materiais = await MateriaisDoModelo.CarregarAsync(unitOfWork, cancellationToken);
        var (pecas, semModelo) = await PecasDaComposicaoAsync(projeto, versao, materiais, cancellationToken);
        if (pecas.Count == 0)
        {
            throw new RegraDeNegocioException("Nenhuma peça da composição tem modelo 3D final. Modele as peças e marque o arquivo final na página de cada uma.");
        }

        if (pecas.Sum(p => p.Quantidade) > MontarAPartirDasPecasCommand.UnidadesMaximo)
        {
            throw new RegraDeNegocioException($"A montagem leva até {MontarAPartirDasPecasCommand.UnidadesMaximo} unidades com modelo 3D.");
        }

        var atual = conversa is null ? null : await ConversaDoEscopo.ArquivoAtualAsync(unitOfWork, conversa.Id, cancellationToken);
        var anterior = atual is { EmBranco: false } ? atual : null;
        var montado = await montador.MontarAsync(anterior?.Conteudo, materiais, pecas, cancellationToken);

        conversa ??= await ConversaDoEscopo.ObterOuCriarAsync(unitOfWork, escopo, Modelo3dNoAssistente.Codigo, agora, cancellationToken);
        var arquivo = ArquivoFerramenta.Criar(conversa.Id, montado, OrigemArquivo.Montagem, agora);
        unitOfWork.GetRepository<ArquivoFerramenta>().Adicionar(arquivo);
        await unitOfWork.SalvarAsync(cancellationToken);

        return new ModeloMontadoDto
        {
            ArquivoId = arquivo.Id,
            ArquivoAnteriorId = anterior?.Id,
            Pecas = [.. pecas.Select(p => new PecaMontadaDto { Nome = p.Nome, Quantidade = p.Quantidade })],
            PecasSemModelo = semModelo,
        };
    }

    /// <summary>
    /// As peças da composição (na ordem dos itens; a mesma peça em dois itens soma as quantidades) com o arquivo final do
    /// Modelo 3D de cada uma, e as que não têm modelo final.
    /// </summary>
    private async Task<(List<PecaParaMontar> ComModelo, List<string> SemModelo)> PecasDaComposicaoAsync(
        Projeto projeto,
        VersaoProjeto versao,
        IReadOnlyList<MaterialDoModelo> materiais,
        CancellationToken cancellationToken)
    {
        var comModelo = new List<PecaParaMontar>();
        var semModelo = new List<string>();
        foreach (var item in versao.Itens.OrderBy(i => i.Id).GroupBy(i => i.PecaDoProjetoId))
        {
            var peca = projeto.Pecas.Single(p => p.Id == item.Key);
            var conversaId = await ConversaDoEscopo.IdAsync(unitOfWork, EscopoDaFerramenta.DaPeca(projeto.Id, peca.Id), Modelo3dNoAssistente.Codigo, cancellationToken);
            var arquivo = conversaId is null ? null : await ConversaDoEscopo.ArquivoFinalAsync(unitOfWork, conversaId.Value, cancellationToken);
            if (arquivo is not { EmBranco: false })
            {
                semModelo.Add(peca.Nome);
                continue;
            }

            var materialDaLiga = materiais.FirstOrDefault(m => m.LigaId is not null && m.LigaId == peca.LigaId)?.Nome;
            comModelo.Add(new PecaParaMontar(peca.Nome, arquivo.Conteudo, item.Sum(i => i.Quantidade), materialDaLiga));
        }

        return (comModelo, semModelo);
    }
}

/// <summary>
/// O arquivo montado (<see cref="ArquivoId"/>), o de antes (<see cref="ArquivoAnteriorId"/>, nulo se a versão não tinha
/// modelo; o Desfazer restaura), as peças que entraram e as que ficaram de fora por não terem modelo 3D final.
/// </summary>
public sealed class ModeloMontadoDto
{
    public long ArquivoId { get; init; }

    public long? ArquivoAnteriorId { get; init; }

    public IReadOnlyList<PecaMontadaDto> Pecas { get; init; } = [];

    public IReadOnlyList<string> PecasSemModelo { get; init; } = [];
}

public sealed class PecaMontadaDto
{
    public string Nome { get; init; } = string.Empty;

    public int Quantidade { get; init; }
}
