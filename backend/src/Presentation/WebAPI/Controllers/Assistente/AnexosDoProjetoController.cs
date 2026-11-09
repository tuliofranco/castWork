using Castwork.Application.Assistente;
using Castwork.Application.Assistente.Queries;

using Microsoft.AspNetCore.Mvc;

namespace Castwork.WebAPI.Controllers.Assistente;

/// <summary>
/// O que pode ser anexado "Do projeto" nas conversas com a IA: os arquivos das ferramentas e o PDF do briefing.
/// </summary>
[Route("api/projetos/{projetoId:long}/anexos")]
[Tags("Ferramentas")]
public sealed class AnexosDoProjetoController : ApiController
{
    [HttpGet]
    [EndpointSummary("Arquivos do projeto que podem ser anexados numa conversa (o atual de cada ferramenta e o último PDF do briefing).")]
    [ProducesResponseType<IReadOnlyList<AnexoDoProjetoDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromRoute] long projetoId, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetAnexosDoProjetoQuery { ProjetoId = projetoId }, cancellationToken));
}
