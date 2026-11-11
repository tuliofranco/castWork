using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Entities.Projetos;

namespace Castwork.Application.Assistente;

/// <summary>
/// Os arquivos das ferramentas como anexo: de cada conversa do projeto (uma por ferramenta e versão, ou por ferramenta e
/// peça do projeto), só o arquivo atual, sem os arquivados e sem o desenho em branco.
/// </summary>
public sealed class ArquivosDasFerramentasComoAnexo(IUnitOfWork unitOfWork) : IFonteDeAnexos
{
    public const string Codigo = "arquivo";

    public string Origem => Codigo;

    public async Task<IReadOnlyList<AnexoDoProjetoDto>> ListarAsync(long projetoId, CancellationToken cancellationToken)
    {
        var conversas = await unitOfWork.GetRepository<ConversaFerramenta>()
            .FindBy(c => c.ProjetoId == projetoId)
            .Select(c => new { c.Id, c.Ferramenta, c.VersaoId })
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var ids = conversas.Select(c => c.Id).ToList();

        var atuais = await unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => ids.Contains(a.ConversaId) && a.ArquivadoEm == null)
            .GroupBy(a => a.ConversaId)
            .Select(g => g.OrderByDescending(a => a.Id).Select(a => new { a.Id, a.ConversaId, a.Nome, a.CriadoEm, Tamanho = a.Conteudo.Length }).First())
            .ToListAsync(cancellationToken);

        var numeros = await NumerosDasVersoesAsync(projetoId, cancellationToken);
        return atuais
            .Where(a => a.Tamanho > 0)
            .Select(a =>
            {
                var conversa = conversas.Single(c => c.Id == a.ConversaId);
                int? versao = conversa.VersaoId is { } v && numeros.TryGetValue(v, out var numero) ? numero : null;
                return new AnexoDoProjetoDto(Codigo, a.Id, a.Nome ?? $"{conversa.Ferramenta}-{a.Id}", conversa.Ferramenta, versao, a.CriadoEm, a.Tamanho);
            })
            .OrderByDescending(a => a.CriadoEm)
            .ToList();
    }

    public async Task<AnexoCarregado?> CarregarAsync(long projetoId, long id, CancellationToken cancellationToken)
    {
        var conversas = unitOfWork.GetRepository<ConversaFerramenta>().FindBy(c => c.ProjetoId == projetoId);
        var arquivo = await unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => a.Id == id && a.ArquivadoEm == null && a.Conteudo.Length > 0 && conversas.Any(c => c.Id == a.ConversaId))
            .Select(a => new { a.Id, a.Nome, a.Conteudo, Conversa = conversas.Where(c => c.Id == a.ConversaId).Select(c => new { c.Ferramenta, c.VersaoId, c.PecaDoProjetoId }).First() })
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
        if (arquivo is null)
        {
            return null;
        }

        var numeros = await NumerosDasVersoesAsync(projetoId, cancellationToken);
        var pecaId = arquivo.Conversa.PecaDoProjetoId;
        var peca = pecaId is null
            ? null
            : await unitOfWork.GetRepository<Projeto>()
                .FindBy(p => p.Id == projetoId)
                .SelectMany(p => p.Pecas)
                .Where(p => p.Id == pecaId)
                .Select(p => p.Nome)
                .FirstOrDefaultAsync(cancellationToken);
        var descricao = arquivo.Conversa.VersaoId is { } v && numeros.TryGetValue(v, out var numero) ? $"{arquivo.Conversa.Ferramenta} · versão {numero}"
            : peca is not null ? $"{arquivo.Conversa.Ferramenta} · peça {peca}"
            : arquivo.Conversa.Ferramenta;
        return new AnexoCarregado(
            arquivo.Nome ?? $"{arquivo.Conversa.Ferramenta}-{arquivo.Id}",
            descricao,
            arquivo.Conversa.Ferramenta,
            null,
            arquivo.Conteudo);
    }

    private async Task<Dictionary<long, int>> NumerosDasVersoesAsync(long projetoId, CancellationToken cancellationToken)
        => await unitOfWork.GetRepository<Projeto>()
            .FindBy(p => p.Id == projetoId)
            .SelectMany(p => p.Versoes)
            .Select(v => new { v.Id, v.Numero })
            .AsNoTracking()
            .ToDictionaryAsync(v => v.Id, v => v.Numero, cancellationToken);
}
