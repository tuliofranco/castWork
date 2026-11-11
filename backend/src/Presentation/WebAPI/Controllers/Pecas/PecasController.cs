using Castwork.Application.Assistente.Queries;
using Castwork.Application.Pecas.Commands.AlterarPeca;
using Castwork.Application.Pecas.Commands.CriarPeca;
using Castwork.Application.Pecas.Queries;

using Microsoft.AspNetCore.Mvc;

namespace Castwork.WebAPI.Controllers.Pecas;

/// <summary>
/// Escrita da biblioteca de peças e o conteúdo dos desenhos dela. A leitura é pelo GraphQL (<c>pecas</c> e <c>peca</c>). A tela não usa estas rotas: a
/// biblioteca é povoada pela publicação das peças do projeto (<c>api/projetos/{id}/pecas/{pecaId}/publicar</c>).
/// </summary>
[Route("api/pecas")]
[Tags("Peças")]
public sealed class PecasController : ApiController
{
    [HttpPost]
    [EndpointSummary("Cadastra uma peça na biblioteca (elo, fecho, pedra...).")]
    [ProducesResponseType<IdCriado>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar([FromBody] CriarPecaCommand command, CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, new IdCriado(await Mediator.Send(command, cancellationToken)));

    [HttpPut("{pecaId:long}")]
    [EndpointSummary("Altera uma peça ativa da biblioteca.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Alterar([FromRoute] long pecaId, [FromBody] AlterarPecaCommand command, CancellationToken cancellationToken)
    {
        command.PecaId = pecaId;
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpGet("{pecaId:long}/desenhos/{ferramenta}")]
    [EndpointSummary("O desenho da peça numa ferramenta (desenho-2d: DXF; modelo-3d: JSON do SolidCraft), copiado ao publicar; 204 se não tem.")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/octet-stream")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desenho([FromRoute] long pecaId, [FromRoute] string ferramenta, CancellationToken cancellationToken)
    {
        var conteudo = await Mediator.Send(new GetDesenhoDaPecaQuery { PecaId = pecaId, Ferramenta = ferramenta }, cancellationToken);
        return conteudo is null ? NoContent() : File(conteudo, "application/octet-stream");
    }

    [HttpGet("{pecaId:long}/desenhos/{ferramenta}/previa")]
    [EndpointSummary("A prévia (SVG no 2D, PNG no 3D) do desenho da peça numa ferramenta, para ver sem abrir o editor; 204 se não tem.")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "image/svg+xml")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviaDoDesenho([FromRoute] long pecaId, [FromRoute] string ferramenta, CancellationToken cancellationToken)
    {
        var svg = await Mediator.Send(new GetPreviaDoDesenhoDaPecaQuery { PecaId = pecaId, Ferramenta = ferramenta }, cancellationToken);
        return svg is null ? NoContent() : File(svg, Application.Abstractions.TipoDaPrevia.De(svg));
    }

    [HttpGet("{pecaId:long}/desenhos/{ferramenta}/imagens")]
    [EndpointSummary("As imagens guardadas do desenho da peça numa ferramenta (vista e tipo), copiadas ao publicar.")]
    [ProducesResponseType<IReadOnlyList<ImagemGuardadaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImagensDoDesenho([FromRoute] long pecaId, [FromRoute] string ferramenta, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetImagensDoDesenhoDaPecaQuery { PecaId = pecaId, Ferramenta = ferramenta }, cancellationToken));

    [HttpGet("{pecaId:long}/desenhos/{ferramenta}/imagens/{vista}")]
    [EndpointSummary("Uma imagem guardada do desenho da peça numa vista (iso, frente, topo, lado ou 2d); formato png ou svg, ou a melhor sem ele; 204 se não há.")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "image/png", "image/svg+xml")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImagemDoDesenho([FromRoute] long pecaId, [FromRoute] string ferramenta, [FromRoute] string vista, [FromQuery] string? formato, CancellationToken cancellationToken)
    {
        var imagem = await Mediator.Send(new GetImagemDoDesenhoDaPecaQuery { PecaId = pecaId, Ferramenta = ferramenta, Vista = vista, Formato = formato }, cancellationToken);
        return imagem is null ? NoContent() : File(imagem.Conteudo, imagem.Tipo);
    }
}
