using System.Text.Json;

using Castwork.Application.Abstractions;
using Castwork.Application.Assistente.Commands;
using Castwork.Application.Assistente.Queries;
using Castwork.Application.Common.Pagination;

using Microsoft.AspNetCore.Mvc;

namespace Castwork.WebAPI.Controllers.Assistente;

/// <summary>
/// Ferramentas com IA de uma versão (ex.: <c>desenho-2d</c>): a conversa com a IA daquela ferramenta e o
/// arquivo de trabalho. Cada ferramenta tem a sua conversa e o seu arquivo. As ferramentas do projeto (o
/// briefing, que vale para todas as versões) respondem também sem a versão na rota, e o desenho 2D e o modelo 3D de
/// cada peça do projeto, pela rota da peça (<c>/pecas/{pecaDoProjetoId}</c>), com conversa e arquivo só dela.
/// </summary>
[Route("api/projetos/{projetoId:long}/versoes/{versaoId:long}/ferramentas/{ferramenta}")]
[Route("api/projetos/{projetoId:long}/pecas/{pecaDoProjetoId:long}/ferramentas/{ferramenta}")]
[Route("api/projetos/{projetoId:long}/ferramentas/{ferramenta}")]
[Tags("Ferramentas")]
public sealed class AssistenteController : ApiController
{
    [HttpGet("conversa")]
    [EndpointSummary("A conversa da ferramenta e o Id do arquivo atual (vazia se ainda não começou).")]
    [ProducesResponseType<ConversaFerramentaDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Conversa([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] long pecaDoProjetoId, [FromRoute] string ferramenta, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetConversaFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta }, cancellationToken));

    /// <summary>
    /// Server-Sent Events: cada evento é um <see cref="EventoDoTurno"/> em JSON; o último é o <c>fim</c>.
    /// </summary>
    [HttpGet("conversa/eventos")]
    [EndpointSummary("Acompanha ao vivo o turno da IA (pensamento, passos e texto). Com aguardar, espera alguns segundos o turno começar (logo após enviar a mensagem).")]
    [Produces("text/event-stream")]
    public async Task Eventos(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromRoute] long pecaDoProjetoId,
        [FromRoute] string ferramenta,
        [FromQuery] bool aguardar,
        [FromServices] IPainelDoTurno painel,
        CancellationToken cancellationToken)
    {
        var conversaId = await Mediator.Send(new GetConversaFerramentaIdQuery { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta }, cancellationToken);
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";

        // Sem buffer de proxy (nginx): o evento precisa chegar à tela na hora.
        Response.Headers["X-Accel-Buffering"] = "no";

        // A conversa nasce com a primeira mensagem: aberto junto com o envio, o acompanhamento espera por ela.
        if (aguardar)
        {
            conversaId ??= await EsperarConversaAsync(projetoId, versaoId, pecaDoProjetoId, ferramenta, cancellationToken);
        }

        var terminou = false;
        if (conversaId is { } id)
        {
            await foreach (var evento in painel.AcompanharAsync(id, aguardar ? TimeSpan.FromSeconds(15) : TimeSpan.Zero, cancellationToken))
            {
                await Response.WriteAsync($"data: {JsonSerializer.Serialize(evento, JsonSerializerOptions.Web)}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                terminou = evento.Tipo == "fim";
            }
        }

        // Sem turno para acompanhar: a tela recebe o fim e lê a conversa gravada.
        if (!terminou)
        {
            await Response.WriteAsync("data: {\"tipo\":\"fim\"}\n\n", cancellationToken);
        }
    }

    [HttpGet("conversa/chats")]
    [EndpointSummary("Os chats da conversa (o em andamento e os anteriores), do mais recente para o mais antigo.")]
    [ProducesResponseType<IReadOnlyList<ChatDaConversaDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Chats([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] long pecaDoProjetoId, [FromRoute] string ferramenta, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetChatsFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta }, cancellationToken));

    [HttpGet("conversa/chats/{numero:int}")]
    [EndpointSummary("As mensagens de um chat, para leitura.")]
    [ProducesResponseType<ConversaFerramentaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Chat([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] long pecaDoProjetoId, [FromRoute] string ferramenta, [FromRoute] int numero, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetChatFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta, Numero = numero }, cancellationToken));

    [HttpPost("conversa/chats")]
    [EndpointSummary("Novo chat: o atual vai para o histórico e começa outro em branco, no mesmo desenho. Na peça do projeto não há (uma conversa só por ferramenta).")]
    [ProducesResponseType<ConversaFerramentaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> NovoChat([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] long pecaDoProjetoId, [FromRoute] string ferramenta, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new NovoChatFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta }, cancellationToken));

    [HttpPost("conversa/chats/{numero:int}/continuar")]
    [EndpointSummary("Retoma um chat anterior: ele volta a ser o que vai para a IA.")]
    [ProducesResponseType<ConversaFerramentaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ContinuarChat([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] long pecaDoProjetoId, [FromRoute] string ferramenta, [FromRoute] int numero, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new ContinuarChatFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta, Numero = numero }, cancellationToken));

    [HttpPost("conversa")]
    [EndpointSummary("Envia a mensagem (e imagens em base64) à IA da ferramenta; ela trabalha no arquivo pelo MCP.")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    [ProducesResponseType<ConversaFerramentaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Conversar(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromRoute] long pecaDoProjetoId,
        [FromRoute] string ferramenta,
        [FromBody] ConversarFerramentaCommand command,
        CancellationToken cancellationToken)
    {
        (command.ProjetoId, command.VersaoId, command.PecaDoProjetoId, command.Ferramenta) = (projetoId, versaoId, pecaDoProjetoId, ferramenta);
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [HttpDelete("conversa")]
    [EndpointSummary("Apaga o chat em andamento (os anteriores e o arquivo continuam).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Apagar([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] long pecaDoProjetoId, [FromRoute] string ferramenta, CancellationToken cancellationToken)
    {
        await Mediator.Send(new ApagarConversaFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta }, cancellationToken);
        return NoContent();
    }

    [HttpPost("conversa/mensagens/{ordem:int}/desfazer")]
    [EndpointSummary("Desfaz a última ação da IA: o arquivo de antes volta a ser o atual.")]
    [ProducesResponseType<ConversaFerramentaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Desfazer([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] long pecaDoProjetoId, [FromRoute] string ferramenta, [FromRoute] int ordem, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new DesfazerAcaoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta, Ordem = ordem }, cancellationToken));

    [HttpGet("arquivo")]
    [EndpointSummary("O arquivo atual da ferramenta (o DXF do desenho) ou, com final, o arquivo final da peça; 204 se não existe.")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/octet-stream")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Arquivo([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] long pecaDoProjetoId, [FromRoute] string ferramenta, [FromQuery] bool final, CancellationToken cancellationToken)
    {
        var arquivo = await Mediator.Send(new GetArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta, Final = final }, cancellationToken);
        if (arquivo is null)
        {
            return NoContent();
        }

        Response.Headers["X-Arquivo-Id"] = arquivo.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // Nome dado pelo joalheiro (codificado: cabeçalho HTTP não leva acento); o editor abre o arquivo com ele.
        if (arquivo.Nome is not null)
        {
            Response.Headers["X-Arquivo-Nome"] = Uri.EscapeDataString(arquivo.Nome);
        }

        return File(arquivo.Conteudo, "application/octet-stream");
    }

    [HttpPut("arquivo")]
    [EndpointSummary("Grava o arquivo editado na tela; vira o atual. Devolve o Id da nova versão do arquivo.")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    [ProducesResponseType<IdCriado>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SalvarArquivo(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromRoute] long pecaDoProjetoId,
        [FromRoute] string ferramenta,
        [FromBody] SalvarArquivoFerramentaCommand command,
        CancellationToken cancellationToken)
    {
        (command.ProjetoId, command.VersaoId, command.PecaDoProjetoId, command.Ferramenta) = (projetoId, versaoId, pecaDoProjetoId, ferramenta);
        return Ok(new IdCriado(await Mediator.Send(command, cancellationToken)));
    }

    [HttpGet("arquivo/previa")]
    [HttpGet("arquivo/historico/{arquivoId:long}/previa")]
    [EndpointSummary("A prévia (SVG) de um arquivo (o atual, sem arquivoId, ou o final da peça, com final); 204 se não há desenho ou prévia.")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "image/svg+xml")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Previa(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromRoute] long pecaDoProjetoId,
        [FromRoute] string ferramenta,
        [FromRoute] long? arquivoId,
        [FromQuery] bool final,
        CancellationToken cancellationToken)
    {
        var previa = await Mediator.Send(
            new GetPreviaArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta, ArquivoId = arquivoId, Final = final },
            cancellationToken);
        if (previa is null)
        {
            return NoContent();
        }

        Response.Headers["X-Arquivo-Id"] = previa.ArquivoId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return File(previa.Svg, Application.Abstractions.TipoDaPrevia.De(previa.Svg));
    }

    [HttpGet("arquivo/imagens")]
    [EndpointSummary("As imagens guardadas do arquivo atual (ou, com final, do final da peça), geradas depois de salvar; vazia enquanto não existem.")]
    [ProducesResponseType<ImagensDoArquivoDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Imagens([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] long pecaDoProjetoId, [FromRoute] string ferramenta, [FromQuery] bool final, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetImagensDoArquivoQuery { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta, Final = final }, cancellationToken));

    [HttpGet("arquivo/imagens/{vista}")]
    [EndpointSummary("Uma imagem guardada do arquivo atual (ou, com final, do final da peça) numa vista (iso, frente, topo, lado ou 2d); formato png ou svg, ou a melhor sem ele; 204 se não há.")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "image/png", "image/svg+xml")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Imagem(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromRoute] long pecaDoProjetoId,
        [FromRoute] string ferramenta,
        [FromRoute] string vista,
        [FromQuery] string? formato,
        [FromQuery] bool final,
        CancellationToken cancellationToken)
    {
        var imagem = await Mediator.Send(
            new GetImagemDoArquivoQuery { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta, Vista = vista, Formato = formato, Final = final },
            cancellationToken);
        return imagem is null ? NoContent() : File(imagem.Conteudo, imagem.Tipo);
    }

    [HttpGet("arquivo/historico")]
    [EndpointSummary("As versões do arquivo, da mais recente (a atual) para a mais antiga, sem o conteúdo.")]
    [ProducesResponseType<IReadOnlyList<VersaoDoArquivoDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Historico([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] long pecaDoProjetoId, [FromRoute] string ferramenta, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetHistoricoArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta }, cancellationToken));

    [HttpGet("arquivo/historico/{arquivoId:long}")]
    [EndpointSummary("Uma versão do histórico do arquivo; 204 se for o desenho em branco.")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/octet-stream")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VersaoDoArquivo([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] long pecaDoProjetoId, [FromRoute] string ferramenta, [FromRoute] long arquivoId, CancellationToken cancellationToken)
    {
        var arquivo = await Mediator.Send(new GetVersaoArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta, ArquivoId = arquivoId }, cancellationToken);
        return arquivo is null ? NoContent() : File(arquivo.Conteudo, "application/octet-stream");
    }

    [HttpPost("arquivo/historico/{arquivoId:long}/restaurar")]
    [EndpointSummary("Volta a uma versão do histórico: uma cópia dela vira a atual. Devolve o Id novo (nulo se em branco).")]
    [ProducesResponseType<ArquivoRestaurado>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restaurar([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] long pecaDoProjetoId, [FromRoute] string ferramenta, [FromRoute] long arquivoId, CancellationToken cancellationToken)
        => Ok(new ArquivoRestaurado(await Mediator.Send(
            new RestaurarArquivoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta, ArquivoId = arquivoId },
            cancellationToken)));

    [HttpPost("arquivo/historico/{arquivoId:long}/final")]
    [EndpointSummary("Marca o arquivo como o final da peça (o desenho dela naquela ferramenta); no 3D, os corpos recebem o nome da peça numa nova versão do arquivo. Numa peça publicada, trocar o final faz dela outra peça.")]
    [ProducesResponseType<ArquivoFinalDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> MarcarFinal([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] long pecaDoProjetoId, [FromRoute] string ferramenta, [FromRoute] long arquivoId, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(
            new MarcarArquivoFinalFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta, ArquivoId = arquivoId },
            cancellationToken));

    [HttpPut("arquivo/historico/{arquivoId:long}/nome")]
    [EndpointSummary("Renomeia o arquivo (nome vazio volta ao padrão da ferramenta).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Renomear(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromRoute] long pecaDoProjetoId,
        [FromRoute] string ferramenta,
        [FromRoute] long arquivoId,
        [FromBody] RenomearArquivoFerramentaCommand command,
        CancellationToken cancellationToken)
    {
        (command.ProjetoId, command.VersaoId, command.PecaDoProjetoId, command.Ferramenta, command.ArquivoId) = (projetoId, versaoId, pecaDoProjetoId, ferramenta, arquivoId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("arquivo/historico/{arquivoId:long}/arquivar")]
    [EndpointSummary("Arquiva o arquivo; se era o atual, o anterior não arquivado passa a ser. O arquivo final da peça não arquiva.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Arquivar([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] long pecaDoProjetoId, [FromRoute] string ferramenta, [FromRoute] long arquivoId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new ArquivarArquivoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta, ArquivoId = arquivoId }, cancellationToken);
        return NoContent();
    }

    [HttpPost("arquivo/historico/{arquivoId:long}/desarquivar")]
    [EndpointSummary("Volta o arquivo para a pasta.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Desarquivar([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] long pecaDoProjetoId, [FromRoute] string ferramenta, [FromRoute] long arquivoId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DesarquivarArquivoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta, ArquivoId = arquivoId }, cancellationToken);
        return NoContent();
    }

    [HttpDelete("arquivo/historico/{arquivoId:long}")]
    [EndpointSummary("Apaga de vez um arquivo arquivado.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ApagarArquivo([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] long pecaDoProjetoId, [FromRoute] string ferramenta, [FromRoute] long arquivoId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new ApagarArquivoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta, ArquivoId = arquivoId }, cancellationToken);
        return NoContent();
    }

    [HttpGet("~/api/projetos/{projetoId:long}/versoes/{versaoId:long}/ferramentas/arquivos")]
    [EndpointSummary("Os arquivos da versão, de todas as ferramentas, do mais recente para o mais antigo (paginado).")]
    [ProducesResponseType<ListaPaginada<ArquivoDaVersaoDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Arquivos(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromQuery] bool arquivados = false,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10,
        CancellationToken cancellationToken = default)
        => Ok(await Mediator.Send(
            new GetArquivosDaVersaoQuery { ProjetoId = projetoId, VersaoId = versaoId, Arquivados = arquivados, Pagina = pagina, TamanhoPagina = tamanhoPagina },
            cancellationToken));

    [HttpGet("~/api/projetos/{projetoId:long}/versoes/{versaoId:long}/ferramentas/imagens/{chave:guid}")]
    [HttpGet("~/api/projetos/{projetoId:long}/pecas/{pecaDoProjetoId:long}/ferramentas/imagens/{chave:guid}")]
    [HttpGet("~/api/projetos/{projetoId:long}/ferramentas/imagens/{chave:guid}")]
    [EndpointSummary("Uma imagem das conversas das ferramentas da versão ou da peça (anexada ou gerada pela IA).")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "image/png", "image/jpeg", "image/webp")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Imagem([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] long pecaDoProjetoId, [FromRoute] Guid chave, CancellationToken cancellationToken)
    {
        var imagem = await Mediator.Send(new GetImagemConversaQuery { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Chave = chave }, cancellationToken);
        return File(imagem.Conteudo, imagem.Tipo);
    }

    private async Task<long?> EsperarConversaAsync(long projetoId, long versaoId, long pecaDoProjetoId, string ferramenta, CancellationToken cancellationToken)
    {
        for (var tentativa = 0; tentativa < 20; tentativa++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            if (await Mediator.Send(new GetConversaFerramentaIdQuery { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaDoProjetoId, Ferramenta = ferramenta }, cancellationToken) is { } id)
            {
                return id;
            }
        }

        return null;
    }
}
