using Castwork.Domain.Exceptions;

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
        if (_itens.Count == 0)
        {
            throw new RegraDeNegocioException("Adicione ao menos uma peça antes de publicar a versão.");
        }

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

    private ItemComposicao ObterItem(long itemId)
        => _itens.SingleOrDefault(i => i.Id == itemId) ?? throw new EntidadeNaoEncontradaException("Item da composição", itemId);

    private void GarantirEmDesenvolvimento()
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
}
