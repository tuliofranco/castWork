using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Acesso;

using MediatR;

using RegrasDoSistema = Castwork.Application.Common.Security.Regras;

namespace Castwork.Application.Acesso.Commands.CriarPerfil;

[Permissao(RegrasDoSistema.Acesso.GerenciarPerfis)]
public sealed class CriarPerfilCommand : IRequest<long>
{
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// GUIDs do catálogo <see cref="RegrasDoSistema"/>.
    /// </summary>
    public List<Guid> Regras { get; set; } = [];
}

public sealed class CriarPerfilCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CriarPerfilCommand, long>
{
    public async Task<long> Handle(CriarPerfilCommand request, CancellationToken cancellationToken)
    {
        var perfil = Perfil.Criar(request.Nome, request.Regras);

        unitOfWork.GetRepository<Perfil>().Adicionar(perfil);
        await unitOfWork.SalvarAsync(cancellationToken);

        return perfil.Id;
    }
}
