using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Atelie;

using Mapster;

using MapsterMapper;

using MediatR;

namespace Castwork.Application.Atelie.Queries.GetConfiguracaoAtelie;

/// <summary>
/// Qualquer usuário logado lê os dados do ateliê (cabeçalho e PDFs).
/// </summary>
[SomenteAutenticado]
public sealed class GetConfiguracaoAtelieQuery : IRequest<ConfiguracaoAtelieDto>;

public sealed class GetConfiguracaoAtelieQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetConfiguracaoAtelieQuery, ConfiguracaoAtelieDto>
{
    public async Task<ConfiguracaoAtelieDto> Handle(GetConfiguracaoAtelieQuery request, CancellationToken cancellationToken)
        => await unitOfWork.GetRepository<ConfiguracaoAtelie>()
            .FindBy(c => c.Id == ConfiguracaoAtelie.IdUnico)
            .ProjectToType<ConfiguracaoAtelieDto>(mapper.Config)
            .AsNoTracking()
            .SingleAsync(cancellationToken);
}
