using Castwork.Application.Abstractions;
using Castwork.Application.Assistente.Commands;
using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;

namespace Castwork.Application.Assistente;

/// <summary>
/// Acesso à conversa da ferramenta pela rota: a versão ou a peça precisa ser do projeto informado.
/// </summary>
internal static class ConversaDoEscopo
{
    /// <summary>
    /// A versão da conversa pela rota (0 vira nulo: projeto ou peça).
    /// </summary>
    public static long? VersaoDaRota(long versaoId) => versaoId == 0 ? null : versaoId;

    /// <summary>
    /// A peça do projeto da conversa pela rota (0 vira nulo: projeto ou versão).
    /// </summary>
    public static long? PecaDaRota(long pecaDoProjetoId) => pecaDoProjetoId == 0 ? null : pecaDoProjetoId;

    /// <summary>
    /// O projeto existe e, na rota da versão ou da peça, ela é dele. Que a ferramenta seja mesmo daquele escopo, o validador
    /// garante (<see cref="FerramentaValidatorExtensions"/>).
    /// </summary>
    public static async Task GarantirEscopoAsync(IUnitOfWork unitOfWork, IEscopoDaFerramenta escopo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(escopo);
        var (projetoId, versaoId, pecaId) = (escopo.ProjetoId, escopo.VersaoId, escopo.PecaDoProjetoId);
        var existe = await unitOfWork.GetRepository<Projeto>()
            .FindBy(p => p.Id == projetoId
                && (versaoId == 0 || p.Versoes.Any(v => v.Id == versaoId))
                && (pecaId == 0 || p.Pecas.Any(peca => peca.Id == pecaId)))
            .AnyAsync(cancellationToken);
        if (!existe)
        {
            throw versaoId != 0 ? new EntidadeNaoEncontradaException("Versão", versaoId)
                : pecaId != 0 ? new EntidadeNaoEncontradaException("Peça do projeto", pecaId)
                : new EntidadeNaoEncontradaException("Projeto", projetoId);
        }
    }

    public static Task<ConversaFerramenta?> ObterAsync(IUnitOfWork unitOfWork, IEscopoDaFerramenta escopo, string ferramenta, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(escopo);
        var (projetoId, versao, peca) = (escopo.ProjetoId, VersaoDaRota(escopo.VersaoId), PecaDaRota(escopo.PecaDoProjetoId));
        return unitOfWork.GetRepository<ConversaFerramenta>()
            .FindBy(c => c.ProjetoId == projetoId && c.VersaoId == versao && c.PecaDoProjetoId == peca && c.Ferramenta == ferramenta)
            .Include(c => c.Mensagens)
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// O Id da conversa, sem carregá-la (nulo se ainda não começou).
    /// </summary>
    public static Task<long?> IdAsync(IUnitOfWork unitOfWork, IEscopoDaFerramenta escopo, string ferramenta, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(escopo);
        var (projetoId, versao, peca) = (escopo.ProjetoId, VersaoDaRota(escopo.VersaoId), PecaDaRota(escopo.PecaDoProjetoId));
        return unitOfWork.GetRepository<ConversaFerramenta>()
            .FindBy(c => c.ProjetoId == projetoId && c.VersaoId == versao && c.PecaDoProjetoId == peca && c.Ferramenta == ferramenta)
            .Select(c => (long?)c.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// A conversa nasce na primeira mensagem ou no primeiro salvamento do arquivo, e já vai para o banco:
    /// o arquivo e as imagens precisam do Id dela.
    /// </summary>
    public static async Task<ConversaFerramenta> ObterOuCriarAsync(IUnitOfWork unitOfWork, IEscopoDaFerramenta escopo, string ferramenta, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(escopo);
        if (await ObterAsync(unitOfWork, escopo, ferramenta, cancellationToken) is { } existente)
        {
            return existente;
        }

        var conversa = ConversaFerramenta.Criar(escopo.ProjetoId, VersaoDaRota(escopo.VersaoId), ferramenta, agora, PecaDaRota(escopo.PecaDoProjetoId));
        unitOfWork.GetRepository<ConversaFerramenta>().Adicionar(conversa);
        await unitOfWork.SalvarAsync(cancellationToken);
        return conversa;
    }

    /// <summary>
    /// O arquivo atual é a versão mais recente que não está arquivada. Pode estar em branco (<see cref="ArquivoFerramenta.EmBranco"/>):
    /// então não há desenho.
    /// </summary>
    public static Task<ArquivoFerramenta?> ArquivoAtualAsync(IUnitOfWork unitOfWork, long conversaId, CancellationToken cancellationToken)
        => unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => a.ConversaId == conversaId && a.ArquivadoEm == null)
            .OrderByDescending(a => a.Id)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Um arquivo da conversa da ferramenta do escopo, para alterar (renomear, arquivar, apagar).
    /// </summary>
    public static async Task<ArquivoFerramenta> ObterArquivoAsync(IUnitOfWork unitOfWork, ComandoDeArquivo request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await GarantirEscopoAsync(unitOfWork, request, cancellationToken);
        var conversa = await ObterAsync(unitOfWork, request, request.Ferramenta, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arquivo", request.ArquivoId);
        return await unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => a.Id == request.ArquivoId && a.ConversaId == conversa.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arquivo", request.ArquivoId);
    }

    /// <summary>
    /// O arquivo final da conversa de uma peça do projeto (nulo sem final): o desenho da peça naquela ferramenta.
    /// </summary>
    public static Task<ArquivoFerramenta?> ArquivoFinalAsync(IUnitOfWork unitOfWork, long conversaId, CancellationToken cancellationToken)
    {
        var conversa = unitOfWork.GetRepository<ConversaFerramenta>().FindBy(c => c.Id == conversaId);
        return unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => conversa.Any(c => c.ArquivoFinalId == a.Id))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// O arquivo atual ou, com <paramref name="final"/>, o arquivo final (o que o visualizador da peça mostra).
    /// </summary>
    public static Task<ArquivoFerramenta?> ArquivoAsync(IUnitOfWork unitOfWork, long conversaId, bool final, CancellationToken cancellationToken)
        => final ? ArquivoFinalAsync(unitOfWork, conversaId, cancellationToken) : ArquivoAtualAsync(unitOfWork, conversaId, cancellationToken);
}
