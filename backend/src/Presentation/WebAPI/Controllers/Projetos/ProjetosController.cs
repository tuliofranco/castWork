using Castwork.Application.Projetos.Commands.AdicionarItem;
using Castwork.Application.Projetos.Commands.AlterarItem;
using Castwork.Application.Projetos.Commands.AlterarPrecosDoProjeto;
using Castwork.Application.Projetos.Commands.ArquivarVersao;
using Castwork.Application.Projetos.Commands.AtualizarPrecosDoProjeto;
using Castwork.Application.Projetos.Commands.CriarProjeto;
using Castwork.Application.Projetos.Commands.CriarVersao;
using Castwork.Application.Projetos.Commands.DefinirMaoDeObra;
using Castwork.Application.Projetos.Commands.DefinirMarkupDaVersao;
using Castwork.Application.Projetos.Commands.DefinirMetaisDaVersao;
using Castwork.Application.Projetos.Commands.DefinirServicos;
using Castwork.Application.Projetos.Commands.DesarquivarVersao;
using Castwork.Application.Projetos.Commands.DuplicarVersao;
using Castwork.Application.Projetos.Commands.PublicarVersao;
using Castwork.Application.Projetos.Commands.RegistrarPesoReal;
using Castwork.Application.Projetos.Commands.RemoverItem;
using Castwork.Application.Projetos.Commands.RenomearProjeto;
using Castwork.Application.Projetos.Commands.VincularCliente;

using Microsoft.AspNetCore.Mvc;

namespace Castwork.WebAPI.Controllers.Projetos;

/// <summary>
/// Escrita de projetos, versões, composição e orçamento da versão. Os Ids da rota têm precedência sobre os do corpo.
/// </summary>
[Route("api/projetos")]
[Tags("Projetos")]
public sealed class ProjetosController : ApiController
{
    [HttpPost]
    [EndpointSummary("Cria um projeto, sem versões: a primeira é criada depois, com o nome escolhido.")]
    [ProducesResponseType<IdCriado>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar([FromBody] CriarProjetoCommand command, CancellationToken cancellationToken)
        => Criado(await Mediator.Send(command, cancellationToken));

    [HttpPut("{projetoId:long}/nome")]
    [EndpointSummary("Troca o nome do projeto.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Renomear([FromRoute] long projetoId, [FromBody] RenomearProjetoCommand command, CancellationToken cancellationToken)
    {
        command.ProjetoId = projetoId;
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("{projetoId:long}/cliente")]
    [EndpointSummary("Vincula o projeto a um cliente (ou desvincula com clienteId nulo).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VincularCliente([FromRoute] long projetoId, [FromBody] VincularClienteCommand command, CancellationToken cancellationToken)
    {
        command.ProjetoId = projetoId;
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("{projetoId:long}/precos")]
    [EndpointSummary("Edita os preços só deste projeto (cotações e parâmetros). O ateliê não muda.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AlterarPrecos([FromRoute] long projetoId, [FromBody] AlterarPrecosDoProjetoCommand command, CancellationToken cancellationToken)
    {
        command.ProjetoId = projetoId;
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{projetoId:long}/precos/atualizar")]
    [EndpointSummary("Substitui os preços do projeto pelos valores atuais do ateliê (cotações e parâmetros).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AtualizarPrecos([FromRoute] long projetoId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new AtualizarPrecosDoProjetoCommand { ProjetoId = projetoId }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{projetoId:long}/versoes/{versaoId:long}/arquivar")]
    [EndpointSummary("Arquiva uma versão em desenvolvimento (publicada não pode ser arquivada).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ArquivarVersao([FromRoute] long projetoId, [FromRoute] long versaoId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new ArquivarVersaoCommand { ProjetoId = projetoId, VersaoId = versaoId }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{projetoId:long}/versoes/{versaoId:long}/desarquivar")]
    [EndpointSummary("Devolve uma versão arquivada para desenvolvimento.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DesarquivarVersao([FromRoute] long projetoId, [FromRoute] long versaoId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DesarquivarVersaoCommand { ProjetoId = projetoId, VersaoId = versaoId }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{projetoId:long}/versoes")]
    [EndpointSummary("Cria uma versão vazia com o próximo número.")]
    [ProducesResponseType<IdCriado>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CriarVersao([FromRoute] long projetoId, [FromBody] CriarVersaoCommand command, CancellationToken cancellationToken)
    {
        command.ProjetoId = projetoId;
        return Criado(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("{projetoId:long}/versoes/{versaoId:long}/duplicar")]
    [EndpointSummary("Copia a composição da versão para uma nova versão em desenvolvimento.")]
    [ProducesResponseType<IdCriado>(StatusCodes.Status201Created)]
    public async Task<IActionResult> DuplicarVersao(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromBody] DuplicarVersaoCommand command,
        CancellationToken cancellationToken)
    {
        (command.ProjetoId, command.VersaoId) = (projetoId, versaoId);
        return Criado(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("{projetoId:long}/versoes/{versaoId:long}/publicar")]
    [EndpointSummary("Publica a versão: a composição fica congelada. Recusa se alguma peça dela está em rascunho.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PublicarVersao([FromRoute] long projetoId, [FromRoute] long versaoId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new PublicarVersaoCommand { ProjetoId = projetoId, VersaoId = versaoId }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{projetoId:long}/versoes/{versaoId:long}/itens")]
    [EndpointSummary("Adiciona uma peça do projeto à composição (se já estiver, soma a quantidade).")]
    [ProducesResponseType<IdCriado>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AdicionarItem(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromBody] AdicionarItemCommand command,
        CancellationToken cancellationToken)
    {
        (command.ProjetoId, command.VersaoId) = (projetoId, versaoId);
        return Criado(await Mediator.Send(command, cancellationToken));
    }

    [HttpPut("{projetoId:long}/versoes/{versaoId:long}/itens/{itemId:long}")]
    [EndpointSummary("Altera a quantidade ou a observação de um item da composição.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AlterarItem(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromRoute] long itemId,
        [FromBody] AlterarItemCommand command,
        CancellationToken cancellationToken)
    {
        (command.ProjetoId, command.VersaoId, command.ItemId) = (projetoId, versaoId, itemId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{projetoId:long}/versoes/{versaoId:long}/itens/{itemId:long}")]
    [EndpointSummary("Remove um item da composição.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoverItem([FromRoute] long projetoId, [FromRoute] long versaoId, [FromRoute] long itemId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new RemoverItemCommand { ProjetoId = projetoId, VersaoId = versaoId, ItemId = itemId }, cancellationToken);
        return NoContent();
    }

    [HttpPut("{projetoId:long}/versoes/{versaoId:long}/metais")]
    [EndpointSummary("Define as ligas da versão e o peso estimado de cada uma (substitui a lista).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DefinirMetais(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromBody] DefinirMetaisDaVersaoCommand command,
        CancellationToken cancellationToken)
    {
        (command.ProjetoId, command.VersaoId) = (projetoId, versaoId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("{projetoId:long}/versoes/{versaoId:long}/metais/{ligaId:long}/peso-real")]
    [EndpointSummary("Registra o peso medido na peça pronta para uma liga da versão (nulo apaga). Vale em qualquer status.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegistrarPesoReal(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromRoute] long ligaId,
        [FromBody] RegistrarPesoRealCommand command,
        CancellationToken cancellationToken)
    {
        (command.ProjetoId, command.VersaoId, command.LigaId) = (projetoId, versaoId, ligaId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("{projetoId:long}/versoes/{versaoId:long}/mao-de-obra")]
    [EndpointSummary("Define a mão de obra da versão: por horas (× valor da hora do projeto) ou valor fixo.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DefinirMaoDeObra(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromBody] DefinirMaoDeObraCommand command,
        CancellationToken cancellationToken)
    {
        (command.ProjetoId, command.VersaoId) = (projetoId, versaoId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("{projetoId:long}/versoes/{versaoId:long}/servicos")]
    [EndpointSummary("Define os serviços externos da versão (substitui a lista).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DefinirServicos(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromBody] DefinirServicosCommand command,
        CancellationToken cancellationToken)
    {
        (command.ProjetoId, command.VersaoId) = (projetoId, versaoId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("{projetoId:long}/versoes/{versaoId:long}/markup")]
    [EndpointSummary("Define o markup só desta versão (nulo volta a valer o do projeto).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DefinirMarkup(
        [FromRoute] long projetoId,
        [FromRoute] long versaoId,
        [FromBody] DefinirMarkupDaVersaoCommand command,
        CancellationToken cancellationToken)
    {
        (command.ProjetoId, command.VersaoId) = (projetoId, versaoId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    private ObjectResult Criado(long id) => StatusCode(StatusCodes.Status201Created, new IdCriado(id));
}
