using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Ferramentas.Briefings;

/// <summary>
/// Ações do <see cref="Briefing"/>: formulário inicial e campos refinados pela IA.
/// </summary>
public sealed partial class Briefing
{
    public static Briefing Criar(long projetoId, FormularioBriefing formulario)
    {
        var briefing = new Briefing { ProjetoId = Guard.IdValido(projetoId, nameof(ProjetoId)) };
        briefing.PreencherFormulario(formulario);
        return briefing;
    }

    public void PreencherFormulario(FormularioBriefing formulario)
    {
        TipoJoia = Enum.IsDefined(formulario.TipoJoia)
            ? formulario.TipoJoia
            : throw new ValorInvalidoException(nameof(TipoJoia), "não é um tipo de joia válido");
        OQueOClienteQuer = Guard.Obrigatorio(formulario.OQueOClienteQuer, nameof(OQueOClienteQuer), TextoLongoMaxLength);
        Ocasiao = Guard.Opcional(formulario.Ocasiao, nameof(Ocasiao), TextoCurtoMaxLength);
        Materiais = Guard.Opcional(formulario.Materiais, nameof(Materiais), TextoCurtoMaxLength);
        Medidas = Guard.Opcional(formulario.Medidas, nameof(Medidas), TextoCurtoMaxLength);
        Prazo = formulario.Prazo;
        Orcamento = Guard.Opcional(formulario.Orcamento, nameof(Orcamento), TextoCurtoMaxLength);
    }

    /// <summary>
    /// Grava o que a conversa consolidou. Só os campos informados mudam; os demais ficam como estão.
    /// </summary>
    public void Refinar(RefinamentoBriefing refinamento)
    {
        Materiais = refinamento.Materiais is null ? Materiais : Guard.Opcional(refinamento.Materiais, nameof(Materiais), TextoCurtoMaxLength);
        Medidas = refinamento.Medidas is null ? Medidas : Guard.Opcional(refinamento.Medidas, nameof(Medidas), TextoCurtoMaxLength);
        Prazo = refinamento.Prazo ?? Prazo;
        Orcamento = refinamento.Orcamento is null ? Orcamento : Guard.Opcional(refinamento.Orcamento, nameof(Orcamento), TextoCurtoMaxLength);
        OQueSeraFeito = refinamento.OQueSeraFeito is null ? OQueSeraFeito : Guard.Opcional(refinamento.OQueSeraFeito, nameof(OQueSeraFeito), TextoLongoMaxLength);
        Acabamento = refinamento.Acabamento is null ? Acabamento : Guard.Opcional(refinamento.Acabamento, nameof(Acabamento), TextoCurtoMaxLength);
        Pedras = refinamento.Pedras is null ? Pedras : Guard.Opcional(refinamento.Pedras, nameof(Pedras), TextoCurtoMaxLength);
        Fecho = refinamento.Fecho is null ? Fecho : Guard.Opcional(refinamento.Fecho, nameof(Fecho), TextoCurtoMaxLength);
        Observacoes = refinamento.Observacoes is null ? Observacoes : Guard.Opcional(refinamento.Observacoes, nameof(Observacoes), TextoLongoMaxLength);
    }
}
