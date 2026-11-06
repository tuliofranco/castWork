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
    public static async Task GarantirVersaoAsync(IUnitOfWork unitOfWork, long projetoId, long versaoId, CancellationToken cancellationToken)
    {
        var existe = await unitOfWork.GetRepository<Projeto>()
            .FindBy(p => p.Id == projetoId && p.Versoes.Any(v => v.Id == versaoId))
            .AnyAsync(cancellationToken);
        if (!existe)
        {
            throw new EntidadeNaoEncontradaException("Versão", versaoId);
        }
    }

    public static Task<ConversaFerramenta?> ObterAsync(IUnitOfWork unitOfWork, long versaoId, string ferramenta, CancellationToken cancellationToken)
        => unitOfWork.GetRepository<ConversaFerramenta>()
            .FindBy(c => c.VersaoId == versaoId && c.Ferramenta == ferramenta)
            .Include(c => c.Mensagens)
            .SingleOrDefaultAsync(cancellationToken);

    /// <summary>
    /// A conversa nasce na primeira mensagem ou no primeiro salvamento do arquivo, e já vai para o banco:
    /// o arquivo e as imagens precisam do Id dela.
    /// </summary>
    public static async Task<ConversaFerramenta> ObterOuCriarAsync(IUnitOfWork unitOfWork, long versaoId, string ferramenta, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        if (await ObterAsync(unitOfWork, versaoId, ferramenta, cancellationToken) is { } existente)
        {
            return existente;
        }

        var conversa = ConversaFerramenta.Criar(versaoId, ferramenta, agora);
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
        var conversa = await ObterAsync(unitOfWork, request.VersaoId, request.Ferramenta, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arquivo", request.ArquivoId);
        return await unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => a.Id == request.ArquivoId && a.ConversaId == conversa.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arquivo", request.ArquivoId);
    }
}
