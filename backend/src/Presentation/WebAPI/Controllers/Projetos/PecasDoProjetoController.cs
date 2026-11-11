using Castwork.Application.Projetos.Commands.AlterarPecaDoProjeto;
using Castwork.Application.Projetos.Commands.CriarPecaDoProjeto;
using Castwork.Application.Projetos.Commands.DefinirValorDaPecaDoProjeto;
using Castwork.Application.Projetos.Commands.ImportarPecaDaBiblioteca;
using Castwork.Application.Projetos.Commands.PublicarPecaDoProjeto;
using Castwork.Application.Projetos.Queries.GetFichaTecnicaDaPeca;

using Microsoft.AspNetCore.Mvc;

namespace Castwork.WebAPI.Controllers.Projetos;

/// <summary>
/// Escrita das peças do projeto e os documentos dela (PDF, gerado na hora). A leitura dos dados é pelo GraphQL
/// (<c>projeto.pecas</c>). Os Ids da rota têm precedência sobre os do corpo.
/// </summary>
[Route("api/projetos/{projetoId:long}/pecas")]
[Tags("Peças do projeto")]
public sealed class PecasDoProjetoController : ApiController
{
    [HttpPost]
    [EndpointSummary("Cria uma peça no projeto, em rascunho e sem valor.")]
    [ProducesResponseType<IdCriado>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar([FromRoute] long projetoId, [FromBody] CriarPecaDoProjetoCommand command, CancellationToken cancellationToken)
    {
        command.ProjetoId = projetoId;
        return Criado(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("importar")]
    [EndpointSummary("Importa uma peça da biblioteca: cópia publicada, vinculada a ela e sem valor.")]
    [ProducesResponseType<IdCriado>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Importar([FromRoute] long projetoId, [FromBody] ImportarPecaDaBibliotecaCommand command, CancellationToken cancellationToken)
    {
        command.ProjetoId = projetoId;
        return Criado(await Mediator.Send(command, cancellationToken));
    }

    [HttpPut("{pecaDoProjetoId:long}")]
    [EndpointSummary("Altera os dados da peça. Numa peça publicada, mudar algum dado faz dela outra peça (volta a rascunho).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Alterar(
        [FromRoute] long projetoId,
        [FromRoute] long pecaDoProjetoId,
        [FromBody] AlterarPecaDoProjetoCommand command,
        CancellationToken cancellationToken)
    {
        (command.ProjetoId, command.PecaDoProjetoId) = (projetoId, pecaDoProjetoId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("{pecaDoProjetoId:long}/valor")]
    [EndpointSummary("Define o valor da peça neste projeto (custo de uma unidade); nulo tira o valor.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DefinirValor(
        [FromRoute] long projetoId,
        [FromRoute] long pecaDoProjetoId,
        [FromBody] DefinirValorDaPecaDoProjetoCommand command,
        CancellationToken cancellationToken)
    {
        (command.ProjetoId, command.PecaDoProjetoId) = (projetoId, pecaDoProjetoId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{pecaDoProjetoId:long}/publicar")]
    [EndpointSummary("Publica a peça em rascunho na biblioteca. Devolve o Id da peça nova da biblioteca.")]
    [ProducesResponseType<IdCriado>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Publicar([FromRoute] long projetoId, [FromRoute] long pecaDoProjetoId, CancellationToken cancellationToken)
        => Criado(await Mediator.Send(new PublicarPecaDoProjetoCommand { ProjetoId = projetoId, PecaDoProjetoId = pecaDoProjetoId }, cancellationToken));

    [HttpGet("{pecaDoProjetoId:long}/documentos/ficha-tecnica.pdf")]
    [EndpointSummary("A ficha técnica da peça em PDF, gerada na hora (sem guardar). O valor no projeto só aparece para quem tem precificacao.ler.")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> FichaTecnica([FromRoute] long projetoId, [FromRoute] long pecaDoProjetoId, CancellationToken cancellationToken)
        => File(
            await Mediator.Send(new GetFichaTecnicaDaPecaQuery { ProjetoId = projetoId, PecaDoProjetoId = pecaDoProjetoId }, cancellationToken),
            "application/pdf",
            "ficha-tecnica.pdf");

    private ObjectResult Criado(long id) => StatusCode(StatusCodes.Status201Created, new IdCriado(id));
}
