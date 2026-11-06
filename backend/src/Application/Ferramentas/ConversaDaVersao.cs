using Castwork.Application.Abstractions;
using Castwork.Application.Ferramentas.Commands;
using Castwork.Domain.Entities.Ferramentas;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;

namespace Castwork.Application.Ferramentas;

/// <summary>
/// Acesso à conversa da ferramenta pela rota: a versão precisa ser do projeto informado.
/// </summary>
internal static class ConversaDaVersao
{
    /// <summary>
    /// <paramref name="versaoId"/> 0 é a rota do projeto (ferramentas do projeto): basta o projeto existir.
    /// </summary>
    public static async Task GarantirVersaoAsync(IUnitOfWork unitOfWork, long projetoId, long versaoId, CancellationToken cancellationToken)
    {
        var existe = await unitOfWork.GetRepository<Projeto>()
            .FindBy(p => p.Id == projetoId && (versaoId == 0 || p.Versoes.Any(v => v.Id == versaoId)))
            .AnyAsync(cancellationToken);
        if (!existe)
        {
            throw versaoId == 0 ? new EntidadeNaoEncontradaException("Projeto", projetoId) : new EntidadeNaoEncontradaException("Versão", versaoId);
        }
    }

    public static Task<ConversaFerramenta?> ObterAsync(IUnitOfWork unitOfWork, long projetoId, long versaoId, string ferramenta, CancellationToken cancellationToken)
    {
        var versao = EscopoDaFerramenta.Versao(ferramenta, versaoId);
        return unitOfWork.GetRepository<ConversaFerramenta>()
            .FindBy(c => c.ProjetoId == projetoId && c.VersaoId == versao && c.Ferramenta == ferramenta)
            .Include(c => c.Mensagens)
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// A conversa nasce na primeira mensagem ou no primeiro salvamento do arquivo, e já vai para o banco:
    /// o arquivo e as imagens precisam do Id dela.
    /// </summary>
    public static async Task<ConversaFerramenta> ObterOuCriarAsync(IUnitOfWork unitOfWork, long projetoId, long versaoId, string ferramenta, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        if (await ObterAsync(unitOfWork, projetoId, versaoId, ferramenta, cancellationToken) is { } existente)
        {
            return existente;
        }

        // Ferramenta da versão chamada pela rota do projeto: não há versão para criar a conversa.
        var versao = EscopoDaFerramenta.Versao(ferramenta, versaoId);
        if (versao == 0)
        {
            throw new EntidadeNaoEncontradaException("Versão", versaoId);
        }

        var conversa = ConversaFerramenta.Criar(projetoId, versao, ferramenta, agora);
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
    /// Um arquivo da conversa da ferramenta da versão, para alterar (renomear, arquivar, apagar).
    /// </summary>
    public static async Task<ArquivoFerramenta> ObterArquivoAsync(IUnitOfWork unitOfWork, ComandoDeArquivo request, CancellationToken cancellationToken)
    {
        await GarantirVersaoAsync(unitOfWork, request.ProjetoId, request.VersaoId, cancellationToken);
        var conversa = await ObterAsync(unitOfWork, request.ProjetoId, request.VersaoId, request.Ferramenta, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arquivo", request.ArquivoId);
        return await unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => a.Id == request.ArquivoId && a.ConversaId == conversa.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arquivo", request.ArquivoId);
    }
}
