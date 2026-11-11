using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Assistente;

using MediatR;

namespace Castwork.Application.Assistente.Commands;

/// <summary>
/// Gera e guarda as imagens de um arquivo novo da ferramenta (<see cref="ImagemDoArquivo"/>). Nenhuma rota envia: só o
/// serviço em segundo plano que consome a <see cref="IFilaDeImagens"/>, fora de uma requisição (por isso sem permissão).
/// Não faz nada se o arquivo sumiu, está em branco ou já tem imagens (copiadas da biblioteca ao importar).
/// </summary>
[SemPermissao]
public sealed class GerarImagensDoArquivoCommand : IRequest
{
    public long ArquivoId { get; set; }
}

public sealed class GerarImagensDoArquivoCommandHandler(IUnitOfWork unitOfWork, IGeradorDeImagens gerador, TimeProvider relogio)
    : IRequestHandler<GerarImagensDoArquivoCommand>
{
    public async Task Handle(GerarImagensDoArquivoCommand request, CancellationToken cancellationToken)
    {
        var arquivoId = request.ArquivoId;
        var arquivo = await unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => a.Id == arquivoId)
            .Select(a => new { a.Conteudo, a.ConversaId })
            .SingleOrDefaultAsync(cancellationToken);
        if (arquivo is not { Conteudo.Length: > 0 } || await TemImagensAsync(arquivoId, cancellationToken))
        {
            return;
        }

        var ferramenta = await unitOfWork.GetRepository<ConversaFerramenta>()
            .FindBy(c => c.Id == arquivo.ConversaId)
            .Select(c => c.Ferramenta)
            .SingleAsync(cancellationToken);
        var geradas = await gerador.GerarAsync(ferramenta, arquivo.Conteudo, cancellationToken);

        // Enquanto o programa renderizava, as imagens podem ter chegado por outro caminho (a cópia ao importar).
        if (geradas.Count == 0 || await TemImagensAsync(arquivoId, cancellationToken))
        {
            return;
        }

        var agora = relogio.GetUtcNow();
        foreach (var imagem in geradas)
        {
            unitOfWork.GetRepository<ImagemDoArquivo>().Adicionar(ImagemDoArquivo.Criar(arquivoId, imagem.Vista, imagem.Conteudo, agora));
        }

        await unitOfWork.SalvarAsync(cancellationToken);
    }

    private Task<bool> TemImagensAsync(long arquivoId, CancellationToken cancellationToken)
        => unitOfWork.GetRepository<ImagemDoArquivo>().FindBy(i => i.ArquivoId == arquivoId).AnyAsync(cancellationToken);
}
