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
    /// A fala do joalheiro vai para o banco antes da IA começar: quem sai da tela e volta vê a mensagem e
    /// que a IA ainda está trabalhando. O conteúdo completo chega em <see cref="ConfirmarFala"/>.
    /// </summary>
    public MensagemFerramenta RegistrarFala(string? texto, IReadOnlyList<Guid> imagens, DateTimeOffset em)
    {
        if (AguardandoIa(em))
        {
            throw new RegraDeNegocioException("A IA ainda está trabalhando na mensagem anterior.");
        }

        DescartarFalaSemResposta();
        return RegistrarMensagem(PapelMensagem.Joalheiro, texto, ConteudoProvisorio, em, imagens);
    }

    /// <summary>
    /// A IA respondeu: a fala ganha o conteúdo completo, como a IA a recebeu.
    /// </summary>
    public void ConfirmarFala(MensagemFerramenta fala, string conteudo)
    {
        if (!_mensagens.Contains(fala))
        {
            throw new RegraDeNegocioException("A mensagem não é desta conversa.");
        }

        fala.DefinirConteudo(conteudo);
    }

    /// <summary>
    /// A IA falhou: a fala sai da conversa, como se não tivesse sido enviada.
    /// </summary>
    public void DescartarFala(MensagemFerramenta fala) => _mensagens.Remove(fala);

    /// <summary>
    /// A última mensagem é uma fala do joalheiro ainda sem resposta, enviada há pouco: a IA está trabalhando nela.
    /// Passado o tempo máximo de um turno, a fala ficou órfã (o servidor caiu no meio) e não trava a conversa.
    /// </summary>
    public bool AguardandoIa(DateTimeOffset agora)
        => _mensagens.MaxBy(m => m.Ordem) is { Papel: PapelMensagem.Joalheiro } ultima && agora - ultima.CriadaEm < TempoMaximoDoTurno;

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

    /// <summary>
    /// Fala órfã (sem resposta e com o turno já expirado) sai antes da próxima: a IA precisa da conversa
    /// alternando joalheiro e assistente.
    /// </summary>
    private void DescartarFalaSemResposta()
    {
        if (_mensagens.MaxBy(m => m.Ordem) is { Papel: PapelMensagem.Joalheiro } orfa)
        {
            _mensagens.Remove(orfa);
        }
    }
}
