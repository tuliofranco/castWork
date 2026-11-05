using Castwork.Application.Abstractions;
using Castwork.Application.Acesso.Common;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Acesso;

using MediatR;

namespace Castwork.Application.Acesso.Commands.Logout;

/// <summary>
/// Encerra a sessão revogando a família do refresh token. Público porque o access token pode já ter
/// expirado; idempotente e sem erro para token desconhecido (não revela nada).
/// </summary>
[SemPermissao]
public sealed class LogoutCommand : IRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class LogoutCommandHandler(IUnitOfWork unitOfWork, ITokenService tokenService, EmissorDeSessao emissorDeSessao)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var hash = tokenService.CalcularHash(request.RefreshToken);
        var familia = await unitOfWork.GetRepository<RefreshToken>()
            .FindBy(t => t.TokenHash == hash)
            .Select(t => (Guid?)t.Familia)
            .SingleOrDefaultAsync(cancellationToken);

        if (familia is not null)
        {
            await emissorDeSessao.RevogarFamiliaAsync(familia.Value, "Logout", cancellationToken);
            await unitOfWork.SalvarAsync(cancellationToken);
        }
    }
}
