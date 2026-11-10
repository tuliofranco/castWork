using Castwork.Application.Precificacao.Commands.AlterarLiga;
using Castwork.Application.Precificacao.Commands.AtualizarCotacao;
using Castwork.Application.Precificacao.Commands.AtualizarParametrosDePreco;
using Castwork.Application.Precificacao.Commands.CriarLiga;
using Castwork.Application.Precificacao.Commands.DesativarLiga;

using Microsoft.AspNetCore.Mvc;

namespace Castwork.WebAPI.Controllers.Precificacao;

/// <summary>
/// Escrita dos preços do ateliê. A leitura é pelo GraphQL (<c>precosDoAtelie</c> e <c>historicoDeCotacoes</c>).
/// </summary>
[Route("api/precificacao")]
[Tags("Precificação")]
public sealed class PrecificacaoController : ApiController
{
    [HttpPost("cotacoes")]
    [EndpointSummary("Registra a cotação do metal puro (R$/g). A anterior fica no histórico.")]
    [ProducesResponseType<IdCriado>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AtualizarCotacao([FromBody] AtualizarCotacaoCommand command, CancellationToken cancellationToken)
        => Criado(await Mediator.Send(command, cancellationToken));

    [HttpPost("ligas")]
    [EndpointSummary("Cadastra uma liga do ateliê (metal, teor e densidade).")]
    [ProducesResponseType<IdCriado>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CriarLiga([FromBody] CriarLigaCommand command, CancellationToken cancellationToken)
        => Criado(await Mediator.Send(command, cancellationToken));

    [HttpPut("ligas/{ligaId:long}")]
    [EndpointSummary("Altera uma liga ativa.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AlterarLiga([FromRoute] long ligaId, [FromBody] AlterarLigaCommand command, CancellationToken cancellationToken)
    {
        command.LigaId = ligaId;
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("ligas/{ligaId:long}/desativar")]
    [EndpointSummary("Desativa uma liga: some da tela e dos orçamentos novos (não há exclusão).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DesativarLiga([FromRoute] long ligaId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DesativarLigaCommand { LigaId = ligaId }, cancellationToken);
        return NoContent();
    }

    [HttpPut("parametros")]
    [EndpointSummary("Atualiza perda, valor da hora, markup, taxas e arredondamento padrão do ateliê.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AtualizarParametros([FromBody] AtualizarParametrosDePrecoCommand command, CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    private ObjectResult Criado(long id) => StatusCode(StatusCodes.Status201Created, new IdCriado(id));
}
