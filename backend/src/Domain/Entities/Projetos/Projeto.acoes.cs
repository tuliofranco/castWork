using Castwork.Domain.Common;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Projetos;

/// <summary>
/// Ações do <see cref="Projeto"/>: dados do projeto, preços, versões e composição de cada versão.
/// </summary>
public sealed partial class Projeto
{
    /// <summary>
    /// O projeto nasce sem versões (a primeira é criada depois, com o nome que o joalheiro escolher) e com os
    /// preços do ateliê daquele momento.
    /// </summary>
    public static Projeto Criar(string nome, string? descricao, long? clienteId, PrecosDoAtelie precosDoAtelie, DateTimeOffset agora)
    {
        var projeto = new Projeto();
        projeto.Atualizar(nome, descricao, clienteId);
        projeto.HerdarPrecos(precosDoAtelie, agora);
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

    /// <summary>
    /// Edita os preços só neste projeto (o ateliê não muda). As cotações informadas substituem as do projeto; a que
    /// mantém o valor mantém a data de origem, a que muda passa a valer de <paramref name="agora"/>.
    /// </summary>
    public void AlterarPrecos(IReadOnlyDictionary<Metal, Dinheiro> cotacoes, ParametrosDePreco parametros, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(cotacoes);
        SubstituirCotacoes(cotacoes.Select(c => (c.Key, c.Value, CotadaEm: agora)), manterDataSeIgual: true);
        ParametrosDePreco = parametros ?? throw new ArgumentNullException(nameof(parametros));
        PrecosAlteradosEm = agora;
    }

    /// <summary>
    /// "Atualizar para os valores atuais": substitui cotações e parâmetros pelos de hoje do ateliê, desfazendo as
    /// edições feitas no projeto.
    /// </summary>
    public void AtualizarPrecosDoAtelie(PrecosDoAtelie precosDoAtelie, DateTimeOffset agora) => HerdarPrecos(precosDoAtelie, agora);

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

        copia.CopiarOrcamentoDe(origem);

        _versoes.Add(copia);
        return copia;
    }

    public void PublicarVersao(long versaoId, DateTimeOffset agora) => ObterVersao(versaoId).Publicar(agora);

    public ItemComposicao AdicionarItem(long versaoId, long pecaId, int quantidade, string? observacao)
        => ObterVersao(versaoId).AdicionarItem(pecaId, quantidade, observacao);

    public void AlterarItem(long versaoId, long itemId, int quantidade, string? observacao)
        => ObterVersao(versaoId).AlterarItem(itemId, quantidade, observacao);

    public void RemoverItem(long versaoId, long itemId) => ObterVersao(versaoId).RemoverItem(itemId);

    /// <summary>
    /// Orçamento da versão: as ligas e o peso estimado de cada uma (substitui a lista).
    /// </summary>
    public void DefinirMetaisDaVersao(long versaoId, IEnumerable<(long LigaId, Peso PesoEstimado)> metais)
        => ObterVersao(versaoId).DefinirMetais(metais);

    public void DefinirMaoDeObraDaVersao(long versaoId, MaoDeObra? maoDeObra) => ObterVersao(versaoId).DefinirMaoDeObra(maoDeObra);

    public void DefinirServicosDaVersao(long versaoId, IEnumerable<ServicoExterno> servicos) => ObterVersao(versaoId).DefinirServicos(servicos);

    public void DefinirMarkupDaVersao(long versaoId, decimal? markup) => ObterVersao(versaoId).DefinirMarkup(markup);

    public void RegistrarPesoReal(long versaoId, long ligaId, Peso? pesoReal) => ObterVersao(versaoId).RegistrarPesoReal(ligaId, pesoReal);

    private void HerdarPrecos(PrecosDoAtelie precosDoAtelie, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(precosDoAtelie);
        SubstituirCotacoes(precosDoAtelie.CotacoesVigentes.Select(c => (c.Metal, c.Valor, CotadaEm: c.RegistradaEm)), manterDataSeIgual: false);

        // Cópia: o projeto não compartilha a instância com o ateliê.
        ParametrosDePreco = precosDoAtelie.Parametros with { };
        PrecosHerdadosEm = agora;
        PrecosAlteradosEm = null;
    }

    /// <summary>
    /// Deixa o projeto exatamente com as cotações dadas: altera as que já existem, cria as novas e remove as ausentes.
    /// </summary>
    private void SubstituirCotacoes(IEnumerable<(Metal Metal, Dinheiro Valor, DateTimeOffset CotadaEm)> novas, bool manterDataSeIgual)
    {
        var lista = novas.ToList();
        if (lista.GroupBy(c => c.Metal).Any(g => g.Count() > 1))
        {
            throw new ValorInvalidoException(nameof(Cotacoes), "cada metal só pode ter uma cotação");
        }

        _cotacoes.RemoveAll(atual => lista.TrueForAll(c => c.Metal != atual.Metal));
        foreach (var (metal, valor, cotadaEm) in lista)
        {
            var atual = _cotacoes.SingleOrDefault(c => c.Metal == metal);
            if (atual is null)
            {
                _cotacoes.Add(new CotacaoDoProjeto(metal, valor, cotadaEm));
            }
            else if (!manterDataSeIgual || atual.Valor != valor)
            {
                atual.Alterar(valor, cotadaEm);
            }
        }
    }

    private int ProximoNumero() => _versoes.Count == 0 ? 1 : _versoes.Max(v => v.Numero) + 1;

    private VersaoProjeto ObterVersao(long versaoId)
        => _versoes.SingleOrDefault(v => v.Id == versaoId) ?? throw new EntidadeNaoEncontradaException("Versão", versaoId);
}
