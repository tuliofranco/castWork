using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Acesso;

using MediatR;

namespace Castwork.Application.Acesso.Commands.CriarUsuario;

[Permissao(Regras.Acesso.GerenciarUsuarios)]
public sealed class CriarUsuarioCommand : IRequest<long>
{
    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Senha inicial. O usuário deve trocá-la no primeiro acesso.
    /// </summary>
    public string Senha { get; set; } = string.Empty;

    public long? PerfilId { get; set; }

    public bool IsAdmin { get; set; }
}

public sealed class CriarUsuarioCommandHandler(IUnitOfWork unitOfWork, ISenhaHasher senhaHasher)
    : IRequestHandler<CriarUsuarioCommand, long>
{
    public async Task<long> Handle(CriarUsuarioCommand request, CancellationToken cancellationToken)
    {
        var usuario = Usuario.Criar(request.Nome, request.Email, senhaHasher.Hash(request.Senha), request.PerfilId, request.IsAdmin);

        unitOfWork.GetRepository<Usuario>().Adicionar(usuario);
        await unitOfWork.SalvarAsync(cancellationToken);

        return usuario.Id;
    }
}
