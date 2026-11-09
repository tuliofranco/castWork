using Anthropic.Models.Beta.Messages;

using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Assistente;

namespace Castwork.Infra.IA.Assistente.Conversa;

/// <summary>
/// Conversão entre o formato guardado no banco (<see cref="ConteudoGuardado"/>) e as mensagens da API,
/// comum ao briefing e às ferramentas.
/// </summary>
internal static class BlocosDaConversa
{
    public static List<BlocoGuardado> Guardar(BetaMessage resposta)
    {
        var blocos = new List<BlocoGuardado>();
        foreach (var bloco in resposta.Content)
        {
            if (bloco.TryPickText(out var texto))
            {
                blocos.Add(new BlocoGuardado("texto", texto.Text));
            }
            else if (bloco.TryPickThinking(out var pensamento))
            {
                blocos.Add(new BlocoGuardado("pensamento", pensamento.Thinking, pensamento.Signature));
            }
            else if (bloco.TryPickRedactedThinking(out var oculto))
            {
                blocos.Add(new BlocoGuardado("pensamentoOculto", oculto.Data));
            }
            else if (bloco.TryPickToolUse(out var uso))
            {
                blocos.Add(new BlocoGuardado("ferramenta", Id: uso.ID, Nome: uso.Name, Entrada: uso.Input.ToDictionary(kv => kv.Key, kv => kv.Value)));
            }
        }

        return blocos;
    }

    /// <summary>
    /// Histórico das falas anteriores só com o que foi dito: a fala do joalheiro e a resposta final da IA.
    /// Os pedidos e resultados de ferramenta (e os renders) ficam de fora; o arquivo atual já traz o estado.
    /// </summary>
    public static IEnumerable<MensagemDaConversa> Enxuto(IReadOnlyList<MensagemDaConversa> historico)
    {
        foreach (var mensagem in historico)
        {
            if (mensagem.Papel == PapelMensagem.Joalheiro)
            {
                yield return mensagem;
            }
            else if (mensagem.Papel == PapelMensagem.Assistente)
            {
                var blocos = ConteudoGuardado.Ler(mensagem.Conteudo).Blocos;
                if (blocos.All(b => b.Tipo != "ferramenta") && blocos.Where(b => b.Tipo == "texto").ToList() is { Count: > 0 } textos)
                {
                    yield return mensagem with { Conteudo = new ConteudoGuardado(textos).Gravar() };
                }
            }
        }
    }

    public static BetaMessageParam Reconstruir(MensagemDaConversa mensagem, IReadOnlyDictionary<Guid, ImagemDaConversa> imagens, IReadOnlyDictionary<Guid, byte[]>? documentos = null)
    {
        var blocos = new List<BetaContentBlockParam>();
        foreach (var bloco in ConteudoGuardado.Ler(mensagem.Conteudo).Blocos)
        {
            BetaContentBlockParam? param = bloco.Tipo switch
            {
                "imagem" => Imagem(imagens, Guid.Parse(bloco.Id!)),
                "documento" => Documento(bloco, documentos),
                "texto" => new BetaTextBlockParam { Text = bloco.Texto! },
                "pensamento" => new BetaThinkingBlockParam { Thinking = bloco.Texto!, Signature = bloco.Assinatura! },
                "pensamentoOculto" => new BetaRedactedThinkingBlockParam { Data = bloco.Texto! },
                "ferramenta" => new BetaToolUseBlockParam { ID = bloco.Id!, Name = bloco.Nome!, Input = bloco.Entrada! },
                "resultado" => Resultado(bloco, imagens),
                _ => null,
            };
            if (param is not null)
            {
                blocos.Add(param);
            }
        }

        return mensagem.Papel == PapelMensagem.Assistente
            ? new BetaAssistantMessageParam { Content = blocos }
            : new BetaUserMessageParam { Content = blocos };
    }

    /// <summary>
    /// A imagem vai inteira (base64) a cada turno: a API não guarda a conversa entre chamadas.
    /// </summary>
    public static BetaContentBlockParam Imagem(IReadOnlyDictionary<Guid, ImagemDaConversa> imagens, Guid chave)
        => imagens.TryGetValue(chave, out var imagem) ? ImagemBase64(imagem) : new BetaTextBlockParam { Text = "[imagem removida]" };

    private static BetaImageBlockParam ImagemBase64(ImagemDaConversa imagem)
        => new()
        {
            Source = new BetaBase64ImageSource
            {
                Data = Convert.ToBase64String(imagem.Conteudo),
                MediaType = imagem.Tipo switch
                {
                    "image/png" => MediaType.ImagePng,
                    "image/webp" => MediaType.ImageWebP,
                    _ => MediaType.ImageJpeg,
                },
            },
        };

    /// <summary>
    /// Resultado de ferramenta: só texto, ou texto e imagens (o render que a IA pediu para conferir o desenho).
    /// </summary>
    private static BetaToolResultBlockParam Resultado(BlocoGuardado bloco, IReadOnlyDictionary<Guid, ImagemDaConversa> imagens)
    {
        if (bloco.Imagens is not { Count: > 0 })
        {
            return new BetaToolResultBlockParam { ToolUseID = bloco.Id!, Content = bloco.Texto ?? string.Empty, IsError = bloco.Erro };
        }

        List<Block> conteudo = [];
        if (!string.IsNullOrEmpty(bloco.Texto))
        {
            conteudo.Add(new BetaTextBlockParam { Text = bloco.Texto });
        }

        foreach (var chave in bloco.Imagens)
        {
            if (imagens.TryGetValue(Guid.Parse(chave), out var imagem))
            {
                conteudo.Add(ImagemBase64(imagem));
            }
        }

        return new BetaToolResultBlockParam { ToolUseID = bloco.Id!, Content = conteudo, IsError = bloco.Erro };
    }

    /// <summary>
    /// PDF anexado do projeto: vai inteiro só no turno em que foi anexado; depois a IA vê só o aviso (o que
    /// ela tirou dele já está na conversa).
    /// </summary>
    private static BetaContentBlockParam Documento(BlocoGuardado bloco, IReadOnlyDictionary<Guid, byte[]>? documentos)
        => documentos is not null && documentos.TryGetValue(Guid.Parse(bloco.Id!), out var pdf)
            ? new BetaRequestDocumentBlock { Source = new BetaBase64PdfSource { Data = Convert.ToBase64String(pdf) }, Title = bloco.Texto }
            : new BetaTextBlockParam { Text = $"{bloco.Texto} [PDF lido num turno anterior]" };
}
