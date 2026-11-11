using Castwork.Domain.Common;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Pecas;

/// <summary>
/// Material de uma peça, da biblioteca (<see cref="Peca"/>) ou do projeto (<see cref="Projetos.PecaDoProjeto"/>).
/// Nas peças de metal (todo tipo menos <see cref="TipoPeca.Pedra"/>) é uma <see cref="Liga"/> do ateliê: o Id e uma
/// <b>cópia</b> do nome e do teor do momento da escolha, que não muda se a liga for editada ou desativada depois. Na pedra
/// é texto livre ("Turmalina"), sem liga e sem teor. Sem material: tudo nulo. As peças anteriores à liga ficaram com o
/// texto e o teor como cópia, sem o Id.
/// </summary>
/// <param name="LigaId">A liga escolhida; nula na pedra, sem material ou nas peças anteriores à liga.</param>
/// <param name="Nome">Nome copiado da liga, ou o material livre da pedra.</param>
/// <param name="Teor">Teor copiado da liga; nulo na pedra.</param>
internal readonly record struct MaterialDaPeca(long? LigaId, string? Nome, TeorMetal? Teor)
{
    /// <summary>
    /// O material pedido para a peça. Pedra: o texto livre (liga é recusada). Outros tipos: a liga, que precisa estar
    /// ativa (texto livre é recusado). Na alteração (<paramref name="atual"/> informado), se a peça já era de metal e a
    /// liga pedida é a mesma gravada, a cópia é mantida, ainda que a liga tenha mudado ou esteja desativada.
    /// </summary>
    public static MaterialDaPeca Escolher(TipoPeca tipo, Liga? liga, string? materialLivre, (TipoPeca Tipo, MaterialDaPeca Material)? atual)
    {
        if (tipo == TipoPeca.Pedra)
        {
            return liga is null
                ? new MaterialDaPeca(null, Guard.Opcional(materialLivre, "Material", Peca.MaterialMaxLength), null)
                : throw new ValorInvalidoException("Liga", "não se aplica à pedra: informe o material");
        }

        if (!string.IsNullOrWhiteSpace(materialLivre))
        {
            throw new ValorInvalidoException("Material", "livre só se aplica à pedra: escolha a liga");
        }

        if (atual is { } gravado && gravado.Tipo != TipoPeca.Pedra && gravado.Material.LigaId == liga?.Id)
        {
            return gravado.Material;
        }

        if (liga is null)
        {
            return default;
        }

        return liga.Ativo
            ? new MaterialDaPeca(liga.Id, liga.Nome, liga.Teor)
            : throw new RegraDeNegocioException($"A liga {liga.Nome} está desativada. Escolha uma liga ativa.");
    }
}
