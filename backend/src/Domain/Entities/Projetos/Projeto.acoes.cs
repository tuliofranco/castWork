using Castwork.Domain.Common;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Projetos;

/// <summary>
/// Ações do <see cref="Projeto"/>: dados do projeto, preços, peças do projeto, versões e composição de cada versão.
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
    /// Troca o nome do projeto.
    /// </summary>
    public void Renomear(string nome) => Nome = Guard.Obrigatorio(nome, nameof(Nome), NomeMaxLength);

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
            copia.AdicionarItem(item.PecaDoProjetoId, item.Quantidade, item.Observacao);
        }

        copia.CopiarOrcamentoDe(origem);

        _versoes.Add(copia);
        return copia;
    }

    /// <summary>
    /// Congela a composição. Todas as peças dela precisam estar publicadas na biblioteca: a recusa diz quais estão em
    /// rascunho.
    /// </summary>
    public void PublicarVersao(long versaoId, DateTimeOffset agora)
    {
        var versao = ObterVersao(versaoId);
        versao.GarantirEmDesenvolvimento();

        var rascunhos = versao.Itens
            .Select(i => ObterPeca(i.PecaDoProjetoId))
            .Where(p => p.Status == StatusPecaDoProjeto.Rascunho)
            .Select(p => p.Nome)
            .ToList();
        if (rascunhos.Count > 0)
        {
            throw new RegraDeNegocioException(
                $"A versão {versao.Numero} usa peças em rascunho: {string.Join(", ", rascunhos)}. Publique essas peças antes de publicar a versão.");
        }

        versao.Publicar(agora);
    }

    /// <summary>
    /// Só entram peças ativas deste projeto (a de outro projeto não é encontrada aqui).
    /// </summary>
    public ItemComposicao AdicionarItem(long versaoId, long pecaDoProjetoId, int quantidade, string? observacao)
    {
        var peca = ObterPeca(pecaDoProjetoId);
        if (!peca.Ativo)
        {
            throw new RegraDeNegocioException($"A peça {peca.Nome} está desativada e não entra em composição nova.");
        }

        return ObterVersao(versaoId).AdicionarItem(pecaDoProjetoId, quantidade, observacao);
    }

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

    /// <summary>
    /// Para o que muda a versão fora do agregado (ex.: o material dos corpos no arquivo 3D): publicada ou arquivada recusa.
    /// </summary>
    public void GarantirVersaoEmDesenvolvimento(long versaoId) => ObterVersao(versaoId).GarantirEmDesenvolvimento();

    /// <summary>
    /// Nova peça criada no projeto: nasce em rascunho, sem vínculo com a biblioteca e sem valor. O material é a liga
    /// (ativa; o nome e o teor dela são copiados) ou, na pedra, o texto livre.
    /// </summary>
    public PecaDoProjeto CriarPeca(string nome, TipoPeca tipo, Liga? liga, string? materialLivre, Peso? peso, string? descricao)
    {
        var peca = PecaDoProjeto.Nova(nome, tipo, liga, materialLivre, peso, descricao);
        _pecas.Add(peca);
        return peca;
    }

    /// <summary>
    /// Traz uma cópia da peça da biblioteca, publicada e vinculada a ela, sem valor. A mesma peça da biblioteca não é
    /// importada duas vezes no projeto (a cópia que teve os dados alterados já é outra peça e não conta).
    /// </summary>
    public PecaDoProjeto ImportarPeca(Peca pecaDaBiblioteca)
    {
        ArgumentNullException.ThrowIfNull(pecaDaBiblioteca);
        if (!pecaDaBiblioteca.Ativo)
        {
            throw new RegraDeNegocioException($"A peça {pecaDaBiblioteca.Nome} está desativada na biblioteca.");
        }

        if (_pecas.Exists(p => p.PecaId == pecaDaBiblioteca.Id))
        {
            throw new RegraDeNegocioException($"A peça {pecaDaBiblioteca.Nome} já está neste projeto.");
        }

        var peca = PecaDoProjeto.ImportadaDe(pecaDaBiblioteca);
        _pecas.Add(peca);
        return peca;
    }

    /// <summary>
    /// Altera os dados da peça do projeto. Publicada com algum dado diferente vira outra peça: volta a rascunho e perde
    /// o vínculo (trocar a liga conta); a peça da biblioteca não muda. A mesma liga mantém a cópia do nome e do teor, ainda
    /// que a liga tenha mudado ou esteja desativada. Retorna se voltou a rascunho.
    /// </summary>
    public bool AlterarPeca(long pecaDoProjetoId, string nome, TipoPeca tipo, Liga? liga, string? materialLivre, Peso? peso, string? descricao, bool ativo)
        => ObterPeca(pecaDoProjetoId).AlterarDados(nome, tipo, liga, materialLivre, peso, descricao, ativo);

    /// <summary>
    /// O desenho da peça mudou (um arquivo novo no desenho 2D ou no modelo 3D dela): publicada, vira outra peça, como ao
    /// alterar os dados. Retorna se voltou a rascunho.
    /// </summary>
    public bool AlterarDesenhoDaPeca(long pecaDoProjetoId) => ObterPeca(pecaDoProjetoId).AlterarDesenho();

    /// <summary>
    /// Custo de uma unidade da peça neste projeto (nulo tira o valor). Não muda o status nem o vínculo.
    /// </summary>
    public void DefinirValorDaPeca(long pecaDoProjetoId, Dinheiro? valor) => ObterPeca(pecaDoProjetoId).DefinirValor(valor);

    /// <summary>
    /// Publica a peça em rascunho: cria a peça na biblioteca (sem valor) e vincula a ela. É a única forma de povoar a
    /// biblioteca. Retorna a peça nova da biblioteca, que o chamador grava junto com o projeto.
    /// </summary>
    public Peca PublicarPeca(long pecaDoProjetoId) => ObterPeca(pecaDoProjetoId).Publicar();

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

    private PecaDoProjeto ObterPeca(long pecaDoProjetoId)
        => _pecas.SingleOrDefault(p => p.Id == pecaDoProjetoId) ?? throw new EntidadeNaoEncontradaException("Peça do projeto", pecaDoProjetoId);

    private VersaoProjeto ObterVersao(long versaoId)
        => _versoes.SingleOrDefault(v => v.Id == versaoId) ?? throw new EntidadeNaoEncontradaException("Versão", versaoId);
}
