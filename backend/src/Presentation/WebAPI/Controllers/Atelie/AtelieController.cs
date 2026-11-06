using Castwork.Application.Atelie.Commands.AtualizarConfiguracaoAtelie;
using Castwork.Application.Atelie.Commands.DefinirLogo;
using Castwork.Application.Atelie.Queries.GetLogoAtelie;
using Castwork.Domain.Entities.Atelie;

using Microsoft.AspNetCore.Mvc;

namespace Castwork.WebAPI.Controllers.Atelie;

/// <summary>
/// Dados e marca do ateliê. A leitura dos dados é pelo GraphQL (<c>configuracaoAtelie</c>); o logo, por ser
/// binário, é lido aqui.
/// </summary>
[Route("api/atelie")]
[Tags("Ateliê")]
public sealed class AtelieController : ApiController
{
    [HttpPut]
    [EndpointSummary("Atualiza os dados e a marca do ateliê.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Atualizar([FromBody] AtualizarConfiguracaoAtelieCommand command, CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpGet("logo")]
    [EndpointSummary("Logo do ateliê; 204 se não houver.")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "image/png", "image/jpeg")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ObterLogo(CancellationToken cancellationToken)
        => await Mediator.Send(new GetLogoAtelieQuery(), cancellationToken) is { } logo ? File(logo.Conteudo, logo.Tipo) : NoContent();

    [HttpPut("logo")]
    [EndpointSummary("Envia o logo (PNG ou JPEG até 1 MB), em multipart com o campo 'arquivo'.")]
    [RequestSizeLimit(ConfiguracaoAtelie.LogoTamanhoMaximo + (64 * 1024))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DefinirLogo(IFormFile arquivo, CancellationToken cancellationToken)
    {
        using var memoria = new MemoryStream();
        await arquivo.CopyToAsync(memoria, cancellationToken);
        await Mediator.Send(new DefinirLogoCommand { Conteudo = memoria.ToArray() }, cancellationToken);
        return NoContent();
    }

    [HttpDelete("logo")]
    [EndpointSummary("Remove o logo: os documentos passam a mostrar só o nome.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoverLogo(CancellationToken cancellationToken)
    {
        await Mediator.Send(new DefinirLogoCommand { Conteudo = null }, cancellationToken);
        return NoContent();
    }
}
