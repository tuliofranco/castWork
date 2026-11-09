using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Assistente;

/// <summary>
/// Ações da <see cref="ConversaFerramenta"/>.
/// </summary>
public sealed partial class ConversaFerramenta
{
    /// <summary>
    /// <paramref name="versaoId"/> nulo cria a conversa de uma ferramenta do projeto (como o briefing).
    /// </summary>
    public static ConversaFerramenta Criar(long projetoId, long? versaoId, string ferramenta, DateTimeOffset em) => new()
    {
        ProjetoId = Guard.IdValido(projetoId, nameof(ProjetoId)),
        VersaoId = versaoId is { } v ? Guard.IdValido(v, nameof(VersaoId)) : null,
        Ferramenta = Guard.Obrigatorio(ferramenta, nameof(Ferramenta), FerramentaMaxLength),
        CriadaEm = em,
    };

    public MensagemFerramenta RegistrarMensagem(
        PapelMensagem papel,
        string? texto,
        string conteudo,
        DateTimeOffset em,
        IReadOnlyList<Guid>? imagens = null,
        AcaoDaIa? acao = null,
        DetalhesDoTurno? detalhes = null)
    {
        // A ordem é única na conversa toda (todos os chats): apagar um chat não reaproveita números.
        var ordem = _mensagens.Count == 0 ? 1 : _mensagens.Max(m => m.Ordem) + 1;
        var mensagem = new MensagemFerramenta(ordem, ChatAtual, papel, texto, conteudo, imagens ?? [], acao, em);
        if (detalhes is not null)
        {
            mensagem.DefinirDetalhes(detalhes);
        }

        _mensagens.Add(mensagem);
        return mensagem;
    }

    /// <summary>
    /// A fala do joalheiro vai para o banco antes da IA começar: quem sai da tela e volta vê a mensagem e
    /// que a IA ainda está trabalhando. O conteúdo completo chega em <see cref="ConfirmarFala"/>.
    /// </summary>
    public MensagemFerramenta RegistrarFala(string? texto, IReadOnlyList<Guid> imagens, DateTimeOffset em, IReadOnlyList<AnexoDaMensagem>? anexos = null)
    {
        if (AguardandoIa(em))
        {
            throw new RegraDeNegocioException("A IA ainda está trabalhando na mensagem anterior.");
        }

        DescartarFalaSemResposta();
        var fala = RegistrarMensagem(PapelMensagem.Joalheiro, texto, ConteudoProvisorio, em, imagens);
        fala.DefinirAnexos(anexos ?? []);
        return fala;
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

        if (mensagem.Chat != ChatAtual)
        {
            throw new RegraDeNegocioException("Só dá para desfazer ações do chat em andamento.");
        }

        // A última ação vale entre todos os chats: o desenho é um só, e desfazer por cima de uma mudança mais
        // recente (feita em outro chat) a perderia.
        var ultima = _mensagens.Where(m => m.Acao is not null && !m.AcaoDesfeita).MaxBy(m => m.Ordem);
        if (ultima != mensagem)
        {
            throw new RegraDeNegocioException("Desfaça antes as ações mais recentes.");
        }

        mensagem.MarcarDesfeita();
        return mensagem.Acao.ArquivoAntesId;
    }

    /// <summary>
    /// Apaga o chat em andamento (os anteriores ficam). O desenho não muda: só a conversa com a IA some.
    /// </summary>
    public void Apagar() => _mensagens.RemoveAll(m => m.Chat == ChatAtual);

    /// <summary>
    /// Encerra o chat em andamento (fica no histórico) e começa outro, em branco: a IA não lembra do anterior,
    /// mas continua no mesmo desenho. Com o chat atual ainda vazio, não há o que encerrar.
    /// </summary>
    public void NovoChat(DateTimeOffset agora)
    {
        GarantirSemIaTrabalhando(agora);
        if (!MensagensDoChat.Any())
        {
            return;
        }

        ChatAtual = _mensagens.Max(m => m.Chat) + 1;
    }

    /// <summary>
    /// Retoma um chat anterior: ele volta a ser o que vai para a IA, e o atual fica no histórico.
    /// </summary>
    public void ContinuarChat(int chat, DateTimeOffset agora)
    {
        GarantirSemIaTrabalhando(agora);
        if (!_mensagens.Any(m => m.Chat == chat))
        {
            throw new EntidadeNaoEncontradaException("Chat", chat);
        }

        ChatAtual = chat;
    }

    private void GarantirSemIaTrabalhando(DateTimeOffset agora)
    {
        if (AguardandoIa(agora))
        {
            throw new RegraDeNegocioException("Espere a IA terminar a mensagem atual.");
        }
    }

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
