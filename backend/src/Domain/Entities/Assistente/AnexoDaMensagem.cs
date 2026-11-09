namespace Castwork.Domain.Entities.Assistente;

/// <summary>
/// Um arquivo do projeto anexado a uma fala: de onde veio (<see cref="Origem"/>: <c>arquivo</c> de uma
/// ferramenta ou <c>briefing</c>), o id na origem e o nome que o chat mostra. Só a referência: o arquivo
/// continua na origem, que nunca apaga versões.
/// </summary>
public sealed record AnexoDaMensagem(string Origem, long Id, string Nome);
