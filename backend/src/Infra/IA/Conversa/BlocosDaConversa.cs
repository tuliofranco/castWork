using Anthropic.Models.Beta.Messages;

using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Briefings;

namespace Castwork.Infra.IA.Conversa;

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

    public static BetaMessageParam Reconstruir(MensagemDaConversa mensagem, IReadOnlyDictionary<Guid, ImagemDaConversa> imagens)
    {
        var blocos = new List<BetaContentBlockParam>();
        foreach (var bloco in ConteudoGuardado.Ler(mensagem.Conteudo).Blocos)
        {
            BetaContentBlockParam? param = bloco.Tipo switch
            {
                "imagem" => Imagem(imagens, Guid.Parse(bloco.Id!)),
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
}
