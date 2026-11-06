using Castwork.Application.Common.Pagination;
using Castwork.Application.Ferramentas.Commands;
using Castwork.Application.Ferramentas.Queries;

using Microsoft.AspNetCore.Mvc;

namespace Castwork.WebAPI.Controllers.Ferramentas;

/// <summary>
/// Ferramentas com IA de uma versão (ex.: <c>desenho-2d</c>): a conversa com a IA daquela ferramenta e o
/// arquivo de trabalho. Cada ferramenta tem a sua conversa e o seu arquivo.
/// </summary>
[Route("api/projetos/{projetoId:long}/versoes/{versaoId:long}/ferramentas/{ferramenta}")]
[Tags("Ferramentas")]
public sealed class FerramentasController : ApiController
{
    [HttpGet("conversa")]
    [EndpointSummary("A conversa da ferramenta e o Id do arquivo atual (vazia se ainda não começou).")]
    [ProducesResponseType<ConversaFerramentaDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Conversa([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] string ferramenta, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetConversaFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = ferramenta }, cancellationToken));

    [HttpPost("conversa")]
    [EndpointSummary("Envia a mensagem (e imagens em base64) à IA da ferramenta; ela trabalha no arquivo pelo MCP.")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    [ProducesResponseType<ConversaFerramentaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Conversar(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromRoute] string ferramenta,
        [FromBody] ConversarFerramentaCommand command,
        CancellationToken cancellationToken)
    {
        (command.ProjetoId, command.VersaoId, command.Ferramenta) = (projetoId, versaoId, ferramenta);
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [HttpDelete("conversa")]
    [EndpointSummary("Apaga a conversa da ferramenta (o arquivo continua).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Apagar([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] string ferramenta, CancellationToken cancellationToken)
    {
        await Mediator.Send(new ApagarConversaFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = ferramenta }, cancellationToken);
        return NoContent();
    }

    [HttpPost("conversa/mensagens/{ordem:int}/desfazer")]
    [EndpointSummary("Desfaz a última ação da IA: o arquivo de antes volta a ser o atual.")]
    [ProducesResponseType<ConversaFerramentaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Desfazer([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] string ferramenta, [FromRoute] int ordem, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new DesfazerAcaoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = ferramenta, Ordem = ordem }, cancellationToken));

    [HttpGet("arquivo")]
    [EndpointSummary("O arquivo atual da ferramenta (o DXF do desenho); 204 se ainda não existe.")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/octet-stream")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Arquivo([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] string ferramenta, CancellationToken cancellationToken)
    {
        var arquivo = await Mediator.Send(new GetArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = ferramenta }, cancellationToken);
        if (arquivo is null)
        {
            return NoContent();
        }

        Response.Headers["X-Arquivo-Id"] = arquivo.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return File(arquivo.Conteudo, "application/octet-stream");
    }

    [HttpPut("arquivo")]
    [EndpointSummary("Grava o arquivo editado na tela; vira o atual. Devolve o Id da nova versão do arquivo.")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    [ProducesResponseType<IdCriado>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SalvarArquivo(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromRoute] string ferramenta,
        [FromBody] SalvarArquivoFerramentaCommand command,
        CancellationToken cancellationToken)
    {
        (command.ProjetoId, command.VersaoId, command.Ferramenta) = (projetoId, versaoId, ferramenta);
        return Ok(new IdCriado(await Mediator.Send(command, cancellationToken)));
    }

    [HttpGet("arquivo/previa")]
    [HttpGet("arquivo/historico/{arquivoId:long}/previa")]
    [EndpointSummary("A prévia (SVG) de um arquivo (o atual, sem arquivoId); 204 se não há desenho ou prévia.")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "image/svg+xml")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Previa([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] string ferramenta, [FromRoute] long? arquivoId, CancellationToken cancellationToken)
    {
        var previa = await Mediator.Send(new GetPreviaArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = ferramenta, ArquivoId = arquivoId }, cancellationToken);
        if (previa is null)
        {
            return NoContent();
        }

        Response.Headers["X-Arquivo-Id"] = previa.ArquivoId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return File(previa.Svg, "image/svg+xml");
    }

    [HttpGet("arquivo/historico")]
    [EndpointSummary("As versões do arquivo, da mais recente (a atual) para a mais antiga, sem o conteúdo.")]
    [ProducesResponseType<IReadOnlyList<VersaoDoArquivoDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Historico([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] string ferramenta, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetHistoricoArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = ferramenta }, cancellationToken));

    [HttpGet("arquivo/historico/{arquivoId:long}")]
    [EndpointSummary("Uma versão do histórico do arquivo; 204 se for o desenho em branco.")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/octet-stream")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VersaoDoArquivo([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] string ferramenta, [FromRoute] long arquivoId, CancellationToken cancellationToken)
    {
        var arquivo = await Mediator.Send(new GetVersaoArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = ferramenta, ArquivoId = arquivoId }, cancellationToken);
        return arquivo is null ? NoContent() : File(arquivo.Conteudo, "application/octet-stream");
    }

    [HttpPost("arquivo/historico/{arquivoId:long}/restaurar")]
    [EndpointSummary("Volta a uma versão do histórico: uma cópia dela vira a atual. Devolve o Id novo (nulo se em branco).")]
    [ProducesResponseType<ArquivoRestaurado>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restaurar([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] string ferramenta, [FromRoute] long arquivoId, CancellationToken cancellationToken)
        => Ok(new ArquivoRestaurado(await Mediator.Send(
            new RestaurarArquivoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = ferramenta, ArquivoId = arquivoId },
            cancellationToken)));

    [HttpPut("arquivo/historico/{arquivoId:long}/nome")]
    [EndpointSummary("Renomeia o arquivo (nome vazio volta ao padrão da ferramenta).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Renomear(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromRoute] string ferramenta,
        [FromRoute] long arquivoId,
        [FromBody] RenomearArquivoFerramentaCommand command,
        CancellationToken cancellationToken)
    {
        (command.ProjetoId, command.VersaoId, command.Ferramenta, command.ArquivoId) = (projetoId, versaoId, ferramenta, arquivoId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("arquivo/historico/{arquivoId:long}/arquivar")]
    [EndpointSummary("Arquiva o arquivo; se era o atual, o anterior não arquivado passa a ser.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Arquivar([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] string ferramenta, [FromRoute] long arquivoId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new ArquivarArquivoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = ferramenta, ArquivoId = arquivoId }, cancellationToken);
        return NoContent();
    }

    [HttpPost("arquivo/historico/{arquivoId:long}/desarquivar")]
    [EndpointSummary("Volta o arquivo para a pasta.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Desarquivar([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] string ferramenta, [FromRoute] long arquivoId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DesarquivarArquivoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = ferramenta, ArquivoId = arquivoId }, cancellationToken);
        return NoContent();
    }

    [HttpDelete("arquivo/historico/{arquivoId:long}")]
    [EndpointSummary("Apaga de vez um arquivo arquivado.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ApagarArquivo([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] string ferramenta, [FromRoute] long arquivoId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new ApagarArquivoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = ferramenta, ArquivoId = arquivoId }, cancellationToken);
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
    [EndpointSummary("Uma imagem das conversas das ferramentas da versão (anexada ou gerada pela IA).")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "image/png", "image/jpeg", "image/webp")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Imagem([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] Guid chave, CancellationToken cancellationToken)
    {
        var imagem = await Mediator.Send(new GetImagemConversaQuery { ProjetoId = projetoId, VersaoId = versaoId, Chave = chave }, cancellationToken);
        return File(imagem.Conteudo, imagem.Tipo);
    }
}
