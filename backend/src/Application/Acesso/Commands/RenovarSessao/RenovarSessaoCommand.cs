using Castwork.Application.Abstractions;
using Castwork.Application.Acesso.Common;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Acesso;

using MediatR;

namespace Castwork.Application.Acesso.Commands.RenovarSessao;

/// <summary>
/// Troca um refresh token válido por um novo par de tokens (rotação). O token usado não vale mais.
/// </summary>
[SemPermissao]
public sealed class RenovarSessaoCommand : IRequest<SessaoDto>
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class RenovarSessaoCommandHandler(
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    EmissorDeSessao emissorDeSessao,
    TimeProvider timeProvider)
    : IRequestHandler<RenovarSessaoCommand, SessaoDto>
{
    public async Task<SessaoDto> Handle(RenovarSessaoCommand request, CancellationToken cancellationToken)
    {
        var agora = timeProvider.GetUtcNow();
        var hash = tokenService.CalcularHash(request.RefreshToken);
        var token = await unitOfWork.GetRepository<RefreshToken>()
            .FindBy(t => t.TokenHash == hash)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException(EmissorDeSessao.MensagemSessaoInvalida);

        // Token já trocado sendo usado de novo: alguém tem uma cópia. Derruba a sessão inteira.
        if (token.FoiUsadoOuRevogado)
        {
            await RevogarEFalharAsync(token.Familia, "Reuso de refresh token", cancellationToken);
        }

        if (!token.EstaAtivo(agora))
        {
            throw new UnauthorizedAccessException(EmissorDeSessao.MensagemSessaoInvalida);
        }

        var usuario = await unitOfWork.GetRepository<Usuario>().ObterPorIdAsync(token.UsuarioId, cancellationToken);
        if (usuario is null || !usuario.Ativo || usuario.VersaoSeguranca != token.VersaoSegurancaDoUsuario)
        {
            await RevogarEFalharAsync(token.Familia, "Credenciais do usuário alteradas", cancellationToken);
        }

        token.MarcarComoUsado(agora);
        var sessao = await emissorDeSessao.EmitirAsync(usuario!, token.Familia, cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);

        return sessao;
    }

    private async Task RevogarEFalharAsync(Guid familia, string motivo, CancellationToken cancellationToken)
    {
        await emissorDeSessao.RevogarFamiliaAsync(familia, motivo, cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);
        throw new UnauthorizedAccessException(EmissorDeSessao.MensagemSessaoInvalida);
    }
}
