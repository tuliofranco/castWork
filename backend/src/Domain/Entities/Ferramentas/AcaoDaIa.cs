using Castwork.Domain.Common;

namespace Castwork.Domain.Entities.Ferramentas;

/// <summary>
/// Uma mudança que a IA fez no arquivo da ferramenta: o resumo para o cartão do chat e a versão do arquivo
/// de antes, para o Desfazer.
/// </summary>
public sealed record AcaoDaIa
{
    public const int DescricaoMaxLength = 300;

    public AcaoDaIa(string descricao, long arquivoAntesId)
    {
        Descricao = Guard.Obrigatorio(descricao, nameof(Descricao), DescricaoMaxLength);
        ArquivoAntesId = arquivoAntesId;
    }

    public string Descricao { get; }

    /// <summary>
    /// <see cref="ArquivoFerramenta"/> de antes da ação; 0 quando a ação criou o primeiro arquivo.
    /// </summary>
    public long ArquivoAntesId { get; }
}
