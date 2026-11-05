using Castwork.Application.Clientes.Commands.CriarCliente;

using Microsoft.AspNetCore.Mvc;

namespace Castwork.WebAPI.Controllers.Clientes;

[Route("api/clientes")]
[Tags("Clientes")]
public sealed class ClientesController : ApiController
{
    [HttpPost]
    [EndpointSummary("Cadastra um cliente. Só o nome é obrigatório.")]
    [ProducesResponseType<IdCriado>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar([FromBody] CriarClienteCommand command, CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, new IdCriado(await Mediator.Send(command, cancellationToken)));
}
