using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Projetos;

/// <summary>
/// Ações da <see cref="VersaoProjeto"/>. Internas: só o agregado <see cref="Projeto"/> as chama.
/// </summary>
public sealed partial class VersaoProjeto
{
    /// <summary>
    /// Só versão em desenvolvimento pode ser arquivada: a publicada é registro do que foi mostrado ao cliente.
    /// </summary>
    internal void Arquivar()
    {
        GarantirEmDesenvolvimento();
        Status = StatusVersao.Arquivada;
    }

    internal void Desarquivar()
    {
        if (Status != StatusVersao.Arquivada)
        {
            throw new RegraDeNegocioException($"A versão {Numero} não está arquivada.");
        }

        Status = StatusVersao.EmDesenvolvimento;
    }

    internal void Publicar(DateTimeOffset agora)
    {
        GarantirEmDesenvolvimento();
        Status = StatusVersao.Publicada;
        PublicadaEm = agora;
    }

    /// <summary>
    /// A mesma peça adicionada de novo soma à quantidade da linha existente, em vez de duplicar a linha.
    /// </summary>
    internal ItemComposicao AdicionarItem(long pecaId, int quantidade, string? observacao)
    {
        GarantirEmDesenvolvimento();
        var existente = _itens.SingleOrDefault(i => i.PecaId == pecaId);
        if (existente is not null)
        {
            existente.Alterar(existente.Quantidade + quantidade, observacao ?? existente.Observacao);
            return existente;
        }

        var item = new ItemComposicao(pecaId, quantidade, observacao);
        _itens.Add(item);
        return item;
    }

    internal void AlterarItem(long itemId, int quantidade, string? observacao)
    {
        GarantirEmDesenvolvimento();
        ObterItem(itemId).Alterar(quantidade, observacao);
    }

    internal void RemoverItem(long itemId)
    {
        GarantirEmDesenvolvimento();
        _itens.Remove(ObterItem(itemId));
    }

    /// <summary>
    /// Substitui as ligas da versão. A liga que continua mantém o peso real já registrado.
    /// </summary>
    internal void DefinirMetais(IEnumerable<(long LigaId, Peso PesoEstimado)> metais)
    {
        ArgumentNullException.ThrowIfNull(metais);
        GarantirEmDesenvolvimento();
        var lista = metais.ToList();
        if (lista.Count > MetaisMaximo)
        {
            throw new ValorInvalidoException(nameof(Metais), $"deve ter no máximo {MetaisMaximo} ligas");
        }

        if (lista.GroupBy(m => m.LigaId).Any(g => g.Count() > 1))
        {
            throw new ValorInvalidoException(nameof(Metais), "cada liga só pode aparecer uma vez");
        }

        Metais = [.. lista.Select(m => new MetalDaVersao(m.LigaId, m.PesoEstimado, Metais.FirstOrDefault(a => a.LigaId == m.LigaId)?.PesoReal))];
    }

    internal void DefinirMaoDeObra(MaoDeObra? maoDeObra)
    {
        GarantirEmDesenvolvimento();
        MaoDeObra = maoDeObra;
    }

    internal void DefinirServicos(IEnumerable<ServicoExterno> servicos)
    {
        ArgumentNullException.ThrowIfNull(servicos);
        GarantirEmDesenvolvimento();
        var lista = servicos.ToList();
        Servicos = lista.Count <= ServicosMaximo
            ? lista
            : throw new ValorInvalidoException(nameof(Servicos), $"deve ter no máximo {ServicosMaximo} serviços");
    }

    /// <summary>
    /// Markup só desta versão; nulo volta a valer o do projeto.
    /// </summary>
    internal void DefinirMarkup(decimal? markup)
    {
        GarantirEmDesenvolvimento();
        Markup = markup switch
        {
            null => null,
            > 0 and <= ParametrosDePreco.MarkupMaximo => Math.Round(markup.Value, ParametrosDePreco.CasasDecimais, MidpointRounding.AwayFromZero),
            _ => throw new ValorInvalidoException(nameof(Markup), $"deve ser maior que zero e até {ParametrosDePreco.MarkupMaximo}"),
        };
    }

    /// <summary>
    /// Peso medido na peça pronta (nulo apaga). Vale em qualquer status: a versão publicada é a que vai para a bancada.
    /// </summary>
    internal void RegistrarPesoReal(long ligaId, Peso? pesoReal)
    {
        var metal = Metais.SingleOrDefault(m => m.LigaId == ligaId) ?? throw new EntidadeNaoEncontradaException("Liga da versão", ligaId);
        Metais = [.. Metais.Select(m => m == metal ? metal.ComPesoReal(pesoReal) : m)];
    }

    /// <summary>
    /// Copia o orçamento de outra versão (na duplicação). O peso real não vem: é da peça que foi feita.
    /// </summary>
    internal void CopiarOrcamentoDe(VersaoProjeto origem)
    {
        Metais = [.. origem.Metais.Select(m => m.ComPesoReal(null))];
        MaoDeObra = origem.MaoDeObra;
        Servicos = [.. origem.Servicos];
        Markup = origem.Markup;
    }

    /// <summary>
    /// Publicada é registro do que foi mostrado ao cliente; arquivada precisa ser desarquivada antes.
    /// </summary>
    internal void GarantirEmDesenvolvimento()
    {
        switch (Status)
        {
            case StatusVersao.Publicada:
                throw new RegraDeNegocioException($"A versão {Numero} está publicada e não pode mais ser alterada. Duplique-a para criar uma nova versão.");
            case StatusVersao.Arquivada:
                throw new RegraDeNegocioException($"A versão {Numero} está arquivada. Desarquive-a para alterar.");
            default:
                break;
        }
    }

    private ItemComposicao ObterItem(long itemId)
        => _itens.SingleOrDefault(i => i.Id == itemId) ?? throw new EntidadeNaoEncontradaException("Item da composição", itemId);
}
