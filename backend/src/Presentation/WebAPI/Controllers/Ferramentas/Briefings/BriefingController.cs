using Castwork.Application.Ferramentas.Briefings.Commands.GerarPdfBriefing;
using Castwork.Application.Ferramentas.Briefings.Commands.SalvarFormularioBriefing;
using Castwork.Application.Ferramentas.Briefings.Queries.GetBriefing;
using Castwork.Application.Ferramentas.Briefings.Queries.GetDocumentoBriefing;
using Castwork.Application.Ferramentas.Briefings.Queries.PreVisualizarPdfBriefing;

using Microsoft.AspNetCore.Mvc;

namespace Castwork.WebAPI.Controllers.Ferramentas.Briefings;

/// <summary>
/// Briefing do projeto: formulário e PDFs (a conversa é a da ferramenta "briefing", em /ferramentas/briefing). Os arquivos não cabem bem no
/// GraphQL (texto longo, binário), por isso a leitura também fica aqui.
/// </summary>
[Route("api/projetos/{projetoId:long}/briefing")]
[Tags("Briefing")]
public sealed class BriefingController : ApiController
{
    [HttpGet]
    [EndpointSummary("Briefing do projeto com os PDFs; 204 se o formulário ainda não foi preenchido.")]
    [ProducesResponseType<BriefingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Obter([FromRoute] long projetoId, CancellationToken cancellationToken)
        => await Mediator.Send(new GetBriefingQuery { ProjetoId = projetoId }, cancellationToken) is { } briefing ? Ok(briefing) : NoContent();

    [HttpPut]
    [EndpointSummary("Cria o briefing com o formulário inicial, ou corrige o formulário.")]
    [ProducesResponseType<IdCriado>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SalvarFormulario([FromRoute] long projetoId, [FromBody] SalvarFormularioBriefingCommand command, CancellationToken cancellationToken)
    {
        command.ProjetoId = projetoId;
        return Ok(new IdCriado(await Mediator.Send(command, cancellationToken)));
    }

    [HttpPost("pdf")]
    [EndpointSummary("Gera um novo PDF do briefing com os dados atuais.")]
    [ProducesResponseType<IdCriado>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GerarPdf([FromRoute] long projetoId, CancellationToken cancellationToken)
        => Criado(await Mediator.Send(new GerarPdfBriefingCommand { ProjetoId = projetoId }, cancellationToken));

    [HttpGet("pdf/previa")]
    [EndpointSummary("Gera o PDF com os dados atuais, sem guardar, para conferir antes de salvar.")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> PreVisualizar([FromRoute] long projetoId, CancellationToken cancellationToken)
        => File(await Mediator.Send(new PreVisualizarPdfBriefingQuery { ProjetoId = projetoId }, cancellationToken), "application/pdf");

    [HttpGet("documentos/{documentoId:long}")]
    [EndpointSummary("Baixa um PDF gerado do briefing.")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Baixar([FromRoute] long projetoId, [FromRoute] long documentoId, CancellationToken cancellationToken)
    {
        var arquivo = await Mediator.Send(new GetDocumentoBriefingQuery { ProjetoId = projetoId, DocumentoId = documentoId }, cancellationToken);
        return File(arquivo.Conteudo, "application/pdf", arquivo.NomeArquivo);
    }

    private ObjectResult Criado(long id) => StatusCode(StatusCodes.Status201Created, new IdCriado(id));
}
