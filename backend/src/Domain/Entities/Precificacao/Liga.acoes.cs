using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Precificacao;

/// <summary>
/// Ações da <see cref="Liga"/>.
/// </summary>
public sealed partial class Liga
{
    public static Liga Criar(string nome, Metal metal, TeorMetal teor, decimal densidade)
    {
        var liga = new Liga { Ativo = true };
        liga.Alterar(nome, metal, teor, densidade);
        return liga;
    }

    public void Alterar(string nome, Metal metal, TeorMetal teor, decimal densidade)
    {
        Nome = Guard.Obrigatorio(nome, nameof(Nome), NomeMaxLength);
        Metal = Enum.IsDefined(metal) ? metal : throw new ValorInvalidoException(nameof(Metal), "não é um metal válido");
        Teor = teor;
        Densidade = densidade is > 0 and <= DensidadeMaxima
            ? Math.Round(densidade, DensidadeCasasDecimais, MidpointRounding.AwayFromZero)
            : throw new ValorInvalidoException(nameof(Densidade), $"deve ser maior que zero e até {DensidadeMaxima} g/cm³");
    }

    public void Desativar() => Ativo = false;
}
