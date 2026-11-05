using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Projetos;

/// <summary>
/// Ações do <see cref="Projeto"/>: dados do projeto, versões e composição de cada versão.
/// </summary>
public sealed partial class Projeto
{
    public const string NomeDaPrimeiraVersao = "Versão 1";

    /// <summary>
    /// Todo projeto nasce com a v1 em desenvolvimento, pronta para receber peças.
    /// </summary>
    public static Projeto Criar(string nome, string? descricao, long? clienteId)
    {
        var projeto = new Projeto();
        projeto.Atualizar(nome, descricao, clienteId);
        projeto._versoes.Add(new VersaoProjeto(1, NomeDaPrimeiraVersao, null));
        return projeto;
    }

    public void Atualizar(string nome, string? descricao, long? clienteId)
    {
        Nome = Guard.Obrigatorio(nome, nameof(Nome), NomeMaxLength);
        Descricao = Guard.Opcional(descricao, nameof(Descricao), DescricaoMaxLength);
        ClienteId = clienteId is null ? null : Guard.IdValido(clienteId.Value, nameof(ClienteId));
    }

    /// <summary>
    /// Vincula (ou desvincula, com nulo) o cliente para quem a arte é feita.
    /// </summary>
    public void VincularCliente(long? clienteId)
        => ClienteId = clienteId is null ? null : Guard.IdValido(clienteId.Value, nameof(ClienteId));

    public void ArquivarVersao(long versaoId) => ObterVersao(versaoId).Arquivar();

    public void DesarquivarVersao(long versaoId) => ObterVersao(versaoId).Desarquivar();

    public VersaoProjeto CriarVersao(string nome, string? observacoes)
    {
        var versao = new VersaoProjeto(ProximoNumero(), nome, observacoes);
        _versoes.Add(versao);
        return versao;
    }

    /// <summary>
    /// Nova versão em desenvolvimento com a mesma composição da origem (que pode estar publicada).
    /// É o caminho para alterar uma versão publicada.
    /// </summary>
    public VersaoProjeto DuplicarVersao(long versaoId, string? nome)
    {
        var origem = ObterVersao(versaoId);
        var numero = ProximoNumero();
        var copia = new VersaoProjeto(numero, string.IsNullOrWhiteSpace(nome) ? $"Versão {numero}" : nome, origem.Observacoes);
        foreach (var item in origem.Itens)
        {
            copia.AdicionarItem(item.PecaId, item.Quantidade, item.Observacao);
        }

        _versoes.Add(copia);
        return copia;
    }

    public void PublicarVersao(long versaoId, DateTimeOffset agora) => ObterVersao(versaoId).Publicar(agora);

    public ItemComposicao AdicionarItem(long versaoId, long pecaId, int quantidade, string? observacao)
        => ObterVersao(versaoId).AdicionarItem(pecaId, quantidade, observacao);

    public void AlterarItem(long versaoId, long itemId, int quantidade, string? observacao)
        => ObterVersao(versaoId).AlterarItem(itemId, quantidade, observacao);

    public void RemoverItem(long versaoId, long itemId) => ObterVersao(versaoId).RemoverItem(itemId);

    private int ProximoNumero() => _versoes.Count == 0 ? 1 : _versoes.Max(v => v.Numero) + 1;

    private VersaoProjeto ObterVersao(long versaoId)
        => _versoes.SingleOrDefault(v => v.Id == versaoId) ?? throw new EntidadeNaoEncontradaException("Versão", versaoId);
}
