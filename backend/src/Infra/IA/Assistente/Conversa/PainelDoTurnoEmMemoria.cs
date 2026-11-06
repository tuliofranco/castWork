using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

using Castwork.Application.Abstractions;

namespace Castwork.Infra.IA.Assistente.Conversa;

/// <summary>
/// Painel do turno em memória: serve um ateliê numa instância só da API. Cada turno guarda os eventos já
/// publicados (quem chega no meio recebe tudo desde o começo) e os repassa a cada tela que acompanha.
/// </summary>
internal sealed class PainelDoTurnoEmMemoria : IPainelDoTurno
{
    private static readonly TimeSpan IntervaloDeEspera = TimeSpan.FromMilliseconds(200);

    private readonly ConcurrentDictionary<long, Turno> _turnos = new();

    public void Comecar(long conversaId) => _turnos[conversaId] = new Turno();

    public void Publicar(long conversaId, EventoDoTurno evento)
    {
        if (_turnos.TryGetValue(conversaId, out var turno))
        {
            turno.Publicar(evento);
        }
    }

    public void Terminar(long conversaId)
    {
        if (_turnos.TryRemove(conversaId, out var turno))
        {
            turno.Terminar();
        }
    }

    public async IAsyncEnumerable<EventoDoTurno> AcompanharAsync(long conversaId, TimeSpan esperarInicio, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var limite = DateTimeOffset.UtcNow + esperarInicio;
        Turno? turno;
        while (!_turnos.TryGetValue(conversaId, out turno))
        {
            if (DateTimeOffset.UtcNow >= limite)
            {
                yield break;
            }

            await Task.Delay(IntervaloDeEspera, cancellationToken);
        }

        var canal = turno.Acompanhar();
        await foreach (var evento in canal.Reader.ReadAllAsync(cancellationToken))
        {
            yield return evento;
        }
    }

    private sealed class Turno
    {
        private readonly Lock _trava = new();
        private readonly List<EventoDoTurno> _eventos = [];
        private readonly List<Channel<EventoDoTurno>> _ouvintes = [];
        private bool _terminado;

        public void Publicar(EventoDoTurno evento)
        {
            lock (_trava)
            {
                _eventos.Add(evento);
                foreach (var ouvinte in _ouvintes)
                {
                    ouvinte.Writer.TryWrite(evento);
                }
            }
        }

        public void Terminar()
        {
            lock (_trava)
            {
                Publicar(new EventoDoTurno("fim"));
                _terminado = true;
                _ouvintes.ForEach(o => o.Writer.TryComplete());
            }
        }

        public Channel<EventoDoTurno> Acompanhar()
        {
            var canal = Channel.CreateUnbounded<EventoDoTurno>();
            lock (_trava)
            {
                _eventos.ForEach(e => canal.Writer.TryWrite(e));
                if (_terminado)
                {
                    canal.Writer.TryComplete();
                }
                else
                {
                    _ouvintes.Add(canal);
                }
            }

            return canal;
        }
    }
}
