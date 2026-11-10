using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Entities.Pecas;

namespace Castwork.Application.Projetos;

/// <summary>
/// O desenho (2D e 3D) da peça do projeto vive nas conversas das ferramentas da peça (<see cref="ConversaFerramenta.PecaDoProjetoId"/>):
/// publicar leva os arquivos finais (com as imagens guardadas) para a peça da biblioteca, e importar traz uma cópia deles de
/// volta para o projeto, já marcada como final.
/// </summary>
internal static class DesenhosDaPecaDoProjeto
{
    /// <summary>
    /// O arquivo final de cada ferramenta da peça (a ferramenta sem final fica de fora), sem o desenho em branco, com as
    /// imagens guardadas dele (vazias se ainda não foram geradas).
    /// </summary>
    public static async Task<IReadOnlyList<DesenhoFinal>> FinaisAsync(IUnitOfWork unitOfWork, long pecaDoProjetoId, CancellationToken cancellationToken)
    {
        var conversas = await unitOfWork.GetRepository<ConversaFerramenta>()
            .FindBy(c => c.PecaDoProjetoId == pecaDoProjetoId && c.ArquivoFinalId != null)
            .Select(c => new { c.Ferramenta, c.ArquivoFinalId })
            .ToListAsync(cancellationToken);

        var finais = new List<DesenhoFinal>();
        foreach (var conversa in conversas.OrderBy(c => c.Ferramenta, StringComparer.Ordinal))
        {
            var arquivo = await unitOfWork.GetRepository<ArquivoFerramenta>()
                .FindBy(a => a.Id == conversa.ArquivoFinalId)
                .Select(a => new { a.Id, a.Conteudo })
                .FirstOrDefaultAsync(cancellationToken);
            if (arquivo is { Conteudo.Length: > 0 })
            {
                var imagens = await unitOfWork.GetRepository<ImagemDoArquivo>()
                    .FindBy(i => i.ArquivoId == arquivo.Id)
                    .Select(i => new { i.Vista, i.Conteudo })
                    .ToListAsync(cancellationToken);
                finais.Add(new DesenhoFinal(conversa.Ferramenta, arquivo.Conteudo, [.. imagens.Select(i => (i.Vista, i.Conteudo))]));
            }
        }

        return finais;
    }

    /// <summary>
    /// A cópia do desenho da peça da biblioteca na peça do projeto importada: uma conversa por ferramenta, com o desenho
    /// como o primeiro arquivo (<see cref="OrigemArquivo.Biblioteca"/>). A peça do projeto já precisa estar no banco (a
    /// conversa aponta para ela).
    /// </summary>
    public static async Task CopiarDaBibliotecaAsync(
        IUnitOfWork unitOfWork,
        long projetoId,
        long pecaDoProjetoId,
        IReadOnlyCollection<DesenhoDaPeca> desenhos,
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        if (desenhos.Count == 0)
        {
            return;
        }

        var conversas = desenhos
            .Select(d => (Desenho: d, Conversa: ConversaFerramenta.Criar(projetoId, versaoId: null, d.Ferramenta, agora, pecaDoProjetoId)))
            .ToList();
        conversas.ForEach(c => unitOfWork.GetRepository<ConversaFerramenta>().Adicionar(c.Conversa));
        await unitOfWork.SalvarAsync(cancellationToken);

        var arquivos = conversas
            .Select(c => (c.Desenho, Arquivo: ArquivoFerramenta.Criar(c.Conversa.Id, c.Desenho.Conteudo, OrigemArquivo.Biblioteca, agora)))
            .ToList();
        arquivos.ForEach(a => unitOfWork.GetRepository<ArquivoFerramenta>().Adicionar(a.Arquivo));
        await unitOfWork.SalvarAsync(cancellationToken);
        conversas.ForEach(c => c.Conversa.MarcarArquivoFinal(arquivos.Single(a => a.Arquivo.ConversaId == c.Conversa.Id).Arquivo));

        // As imagens guardadas vêm junto: a geração em segundo plano vê que o arquivo já tem imagens e não refaz. O arquivo
        // final vai no mesmo salvamento.
        arquivos
            .SelectMany(a => a.Desenho.Imagens.Select(i => ImagemDoArquivo.Criar(a.Arquivo.Id, i.Vista, i.Conteudo, agora)))
            .ToList()
            .ForEach(i => unitOfWork.GetRepository<ImagemDoArquivo>().Adicionar(i));
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}

/// <summary>
/// O arquivo final de uma ferramenta da peça do projeto e as imagens guardadas dele (vista e conteúdo).
/// </summary>
internal sealed record DesenhoFinal(string Ferramenta, byte[] Conteudo, IReadOnlyList<(string Vista, byte[] Conteudo)> Imagens);
