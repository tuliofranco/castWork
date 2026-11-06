using Castwork.Domain.Entities.Briefings;

namespace Castwork.Application.Ferramentas.Queries;

/// <summary>
/// A conversa da ferramenta como o chat mostra, e a versão atual do arquivo (nula se ainda não existe).
/// O front compara <see cref="ArquivoId"/> para saber quando recarregar o desenho.
/// </summary>
public sealed record ConversaFerramentaDto(IReadOnlyList<MensagemFerramentaDto> Mensagens, long? ArquivoId);

public sealed record MensagemFerramentaDto(int Ordem, PapelMensagem Papel, string? Texto, IReadOnlyList<Guid> Imagens, AcaoDaIaDto? Acao, DateTimeOffset CriadaEm);

public sealed record AcaoDaIaDto(string Descricao, bool Desfeita);

public sealed record ArquivoDaFerramentaDto(long Id, byte[] Conteudo);

public sealed record ImagemDaFerramentaDto(string Tipo, byte[] Conteudo);

/// <summary>
/// Uma versão do histórico do arquivo; <see cref="Tamanho"/> 0 é o desenho em branco.
/// </summary>
public sealed record VersaoDoArquivoDto(long Id, Domain.Entities.Ferramentas.OrigemArquivo Origem, DateTimeOffset CriadoEm, int Tamanho);
