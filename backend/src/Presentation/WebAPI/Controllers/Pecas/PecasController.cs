using Castwork.Application.Pecas.Commands.AlterarPeca;
using Castwork.Application.Pecas.Commands.CriarPeca;

using Microsoft.AspNetCore.Mvc;

namespace Castwork.WebAPI.Controllers.Pecas;

/// <summary>
/// Escrita da biblioteca de peças. A leitura é pelo GraphQL (<c>pecas</c> e <c>peca</c>).
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
    [EndpointSummary("Altera uma peça ativa da biblioteca, inclusive o custo por unidade (este só com precificacao.ler).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Alterar([FromRoute] long pecaId, [FromBody] AlterarPecaCommand command, CancellationToken cancellationToken)
    {
        command.PecaId = pecaId;
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
