using Castwork.Domain.Common;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Projetos;

/// <summary>
/// Peça usada nas versões de um projeto: nasce no projeto (rascunho) ou é importada da biblioteca (cópia publicada).
/// Tem os mesmos dados da <see cref="Peca"/> da biblioteca e o <see cref="Valor"/> no projeto, que a biblioteca não tem:
/// a mesma peça pode custar diferente em cada projeto. Só é alterada pelo <see cref="Projeto"/>.
/// </summary>
public sealed partial class PecaDoProjeto : BaseEntity<long>
{
    private PecaDoProjeto()
    {
    }

    public string Nome { get; private set; } = string.Empty;

    public TipoPeca Tipo { get; private set; }

    /// <summary>
    /// A liga do ateliê escolhida (peças de metal: todo tipo menos pedra). Nula na pedra, sem liga ou nas peças anteriores
    /// à escolha por liga. Ver <see cref="MaterialDaPeca"/>.
    /// </summary>
    public long? LigaId { get; private set; }

    /// <summary>
    /// Nome da liga copiado quando ela foi escolhida, ou o material livre da pedra. Editar ou desativar a liga depois não
    /// muda a peça.
    /// </summary>
    public string? Material { get; private set; }

    /// <summary>
    /// Teor copiado da liga (750 = 18k). Nulo na pedra.
    /// </summary>
    public TeorMetal? Teor { get; private set; }

    public Peso? Peso { get; private set; }

    public string? Descricao { get; private set; }

    /// <summary>
    /// Desativada não entra em composição nova (a que já está numa versão continua lá).
    /// </summary>
    public bool Ativo { get; private set; }

    /// <summary>
    /// Custo de uma unidade neste projeto (compra ou produção, não o preço de venda). Nulo: ainda sem valor, o que deixa
    /// o preço da versão incompleto.
    /// </summary>
    public Dinheiro? Valor { get; private set; }

    public StatusPecaDoProjeto Status { get; private set; }

    /// <summary>
    /// A peça da biblioteca de que esta é cópia: preenchida ao importar ou ao publicar, apagada quando os dados mudam
    /// (a peça vira outra). Nula em rascunho.
    /// </summary>
    public long? PecaId { get; private set; }

    /// <summary>
    /// A peça criada na biblioteca ao publicar. Só serve para o EF gravar a peça nova e o vínculo
    /// (<see cref="PecaId"/>) na mesma transação; não é carregada na leitura.
    /// </summary>
    private Peca? PecaPublicada { get; set; }
}
