namespace Castwork.Application.Abstractions;

/// <summary>
/// Acompanhamento ao vivo de um turno da IA: o pensamento, os passos (cada ferramenta usada) e o texto da
/// resposta, à medida que acontecem. Quem abre a tela no meio do turno recebe o que já aconteceu e segue
/// acompanhando. Vale só enquanto o turno dura; o resultado final fica gravado na conversa.
/// </summary>
public interface IPainelDoTurno
{
    void Comecar(long conversaId);

    void Publicar(long conversaId, EventoDoTurno evento);

    void Terminar(long conversaId);

    /// <summary>
    /// Os eventos do turno da conversa, do começo até o fim. Sem turno em andamento, espera ele começar
    /// por <paramref name="esperarInicio"/> (a tela abre o acompanhamento junto com o envio da mensagem).
    /// </summary>
    IAsyncEnumerable<EventoDoTurno> AcompanharAsync(long conversaId, TimeSpan esperarInicio, CancellationToken cancellationToken);
}

/// <summary>
/// Um acontecimento do turno. <see cref="Tipo"/>: <c>pensamento</c> e <c>texto</c> (trechos que se somam),
/// <c>passo</c> (<see cref="Passo"/> é o número; <see cref="Estado"/>: andamento, feito ou erro; pode trazer o
/// render em <see cref="ImagemPng"/>, base64) e <c>fim</c>.
/// </summary>
public sealed record EventoDoTurno(string Tipo, string? Texto = null, int? Passo = null, string? Estado = null, string? ImagemPng = null);
