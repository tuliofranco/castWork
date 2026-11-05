using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Atelie;

using MediatR;

namespace Castwork.Application.Atelie.Queries.GetLogoAtelie;

/// <summary>
/// O logo do ateliê, ou nulo se não houver.
/// </summary>
[SomenteAutenticado]
public sealed class GetLogoAtelieQuery : IRequest<LogoDto?>;

public sealed record LogoDto(byte[] Conteudo, string Tipo);

public sealed class GetLogoAtelieQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetLogoAtelieQuery, LogoDto?>
{
    public async Task<LogoDto?> Handle(GetLogoAtelieQuery request, CancellationToken cancellationToken)
        => await unitOfWork.GetRepository<ConfiguracaoAtelie>()
            .FindBy(c => c.Id == ConfiguracaoAtelie.IdUnico && c.Logo != null)
            .Select(c => new LogoDto(c.Logo!, c.LogoTipo!))
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
}
