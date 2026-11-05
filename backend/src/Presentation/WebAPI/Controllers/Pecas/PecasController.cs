using Castwork.Application.Pecas.Commands.CriarPeca;

using Microsoft.AspNetCore.Mvc;

namespace Castwork.WebAPI.Controllers.Pecas;

[Route("api/pecas")]
[Tags("Peças")]
public sealed class PecasController : ApiController
{
    [HttpPost]
    [EndpointSummary("Abre uma peça em briefing para um cliente ativo.")]
    [ProducesResponseType<IdCriado>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar([FromBody] CriarPecaCommand command, CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, new IdCriado(await Mediator.Send(command, cancellationToken)));
}
