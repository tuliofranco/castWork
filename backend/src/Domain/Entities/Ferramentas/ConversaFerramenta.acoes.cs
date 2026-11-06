using Castwork.Domain.Common;
using Castwork.Domain.Entities.Briefings;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Ferramentas;

/// <summary>
/// Ações da <see cref="ConversaFerramenta"/>.
/// </summary>
public sealed partial class ConversaFerramenta
{
    public static ConversaFerramenta Criar(long versaoId, string ferramenta, DateTimeOffset em) => new()
    {
        VersaoId = Guard.IdValido(versaoId, nameof(VersaoId)),
        Ferramenta = Guard.Obrigatorio(ferramenta, nameof(Ferramenta), FerramentaMaxLength),
        CriadaEm = em,
    };

    public MensagemFerramenta RegistrarMensagem(
        PapelMensagem papel,
        string? texto,
        string conteudo,
        DateTimeOffset em,
        IReadOnlyList<Guid>? imagens = null,
        AcaoDaIa? acao = null)
    {
        var mensagem = new MensagemFerramenta(_mensagens.Count + 1, papel, texto, conteudo, imagens ?? [], acao, em);
        _mensagens.Add(mensagem);
        return mensagem;
    }

    /// <summary>
    /// Marca a ação da IA como desfeita e devolve o arquivo que estava antes dela, para voltar a ser o atual.
    /// Só a última ação ainda valendo pode ser desfeita: desfazer uma do meio perderia as seguintes.
    /// </summary>
    public long DesfazerAcao(int ordem)
    {
        var mensagem = _mensagens.SingleOrDefault(m => m.Ordem == ordem) ?? throw new EntidadeNaoEncontradaException("Mensagem", ordem);
        if (mensagem.Acao is null || mensagem.AcaoDesfeita)
        {
            throw new RegraDeNegocioException("Essa mensagem não tem uma ação para desfazer.");
        }

        var ultima = _mensagens.Where(m => m.Acao is not null && !m.AcaoDesfeita).MaxBy(m => m.Ordem);
        if (ultima != mensagem)
        {
            throw new RegraDeNegocioException("Desfaça antes as ações mais recentes.");
        }

        mensagem.MarcarDesfeita();
        return mensagem.Acao.ArquivoAntesId;
    }

    /// <summary>
    /// Começa a conversa do zero. O desenho não muda: só o histórico com a IA some.
    /// </summary>
    public void Apagar() => _mensagens.Clear();
}
