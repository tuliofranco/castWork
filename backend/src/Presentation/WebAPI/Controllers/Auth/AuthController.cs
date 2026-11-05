using Castwork.Application.Abstractions;
using Castwork.Application.Acesso.Commands.AlterarMinhaSenha;
using Castwork.Application.Acesso.Commands.Login;
using Castwork.Application.Acesso.Commands.Logout;
using Castwork.Application.Acesso.Commands.RenovarSessao;
using Castwork.Application.Acesso.Common;
using Castwork.WebAPI.Configuracao;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Castwork.WebAPI.Controllers.Auth;

[Route("api/auth")]
[Tags("Autenticação")]
public sealed class AuthController : ApiController
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(LimitesDeRequisicao.Autenticacao)]
    [EndpointSummary("Entra com e-mail e senha e recebe access token (15 min) e refresh token.")]
    [ProducesResponseType<SessaoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public Task<SessaoDto> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
        => Mediator.Send(command, cancellationToken);

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(LimitesDeRequisicao.Autenticacao)]
    [EndpointSummary("Troca o refresh token por um novo par de tokens. O refresh token usado deixa de valer.")]
    [ProducesResponseType<SessaoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public Task<SessaoDto> Refresh([FromBody] RenovarSessaoCommand command, CancellationToken cancellationToken)
        => Mediator.Send(command, cancellationToken);

    [HttpPost("logout")]
    [AllowAnonymous]
    [EndpointSummary("Encerra a sessão do refresh token informado (em todos os tokens da mesma sessão).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command, CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("alterar-senha")]
    [EndpointSummary("Troca a senha do usuário logado. Outras sessões caem; esta recebe tokens novos.")]
    [ProducesResponseType<SessaoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<SessaoDto> AlterarSenha([FromBody] AlterarMinhaSenhaCommand command, CancellationToken cancellationToken)
        => Mediator.Send(command, cancellationToken);

    [HttpGet("eu")]
    [EndpointSummary("Dados do usuário do token atual.")]
    public object Eu([FromServices] ISessaoService sessao)
        => new { sessao.UsuarioId, sessao.Nome, sessao.PerfilId, sessao.IsAdmin };
}
