using Castwork.Application.Ferramentas.Modelo3d.Commands;
using Castwork.Application.Ferramentas.Modelo3d.Commands.DefinirMaterialDosCorpos;
using Castwork.Application.Ferramentas.Modelo3d.Commands.MontarAPartirDasPecas;
using Castwork.Application.Ferramentas.Modelo3d.Commands.UnirCorposSobrepostos;

using Microsoft.AspNetCore.Mvc;

namespace Castwork.WebAPI.Controllers.Ferramentas.Modelo3d;

/// <summary>
/// Conferência de peso e materiais do modelo 3D da versão (a leitura é pelo GraphQL, <c>VersaoProjeto.conferenciaDoModelo</c>)
/// e "Montar a partir das peças". Cada alteração grava uma nova versão do arquivo 3D e devolve o Id dela e o de antes (para
/// o Desfazer).
/// </summary>
[Route("api/projetos/{projetoId:long}/versoes/{versaoId:long}/modelo-3d")]
[Tags("Modelo 3D")]
public sealed class Modelo3dController : ApiController
{
    [HttpPut("materiais")]
    [EndpointSummary("Define o material de um ou vários corpos (liga ativa, Pedra ou Apoio) no arquivo 3D atual.")]
    [ProducesResponseType<ModeloAlteradoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DefinirMateriais(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromBody] DefinirMaterialDosCorposCommand command,
        CancellationToken cancellationToken)
    {
        (command.ProjetoId, command.VersaoId) = (projetoId, versaoId);
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("unir-corpos")]
    [EndpointSummary("Une corpos de metal da mesma liga que se sobrepõem num corpo só (o primeiro dá o nome).")]
    [ProducesResponseType<ModeloAlteradoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UnirCorpos(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromBody] UnirCorposSobrepostosCommand command,
        CancellationToken cancellationToken)
    {
        (command.ProjetoId, command.VersaoId) = (projetoId, versaoId);
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("montar-a-partir-das-pecas")]
    [EndpointSummary("Traz para o modelo 3D da versão uma cópia do modelo de cada peça da composição, uma por unidade, enfileiradas para posicionar depois.")]
    [ProducesResponseType<ModeloMontadoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> MontarAPartirDasPecas([FromRoute] long projetoId, [FromRoute] long versaoId, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new MontarAPartirDasPecasCommand { ProjetoId = projetoId, VersaoId = versaoId }, cancellationToken));
}
