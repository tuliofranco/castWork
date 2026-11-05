using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Clientes;
using Castwork.Domain.Entities.Projetos;

using FluentValidation;

using MediatR;

namespace Castwork.Application.Projetos.Commands.VincularCliente;

/// <summary>
/// Vincula o projeto a um cliente ativo, ou desvincula com <c>clienteId</c> nulo (projeto autoral).
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class VincularClienteCommand : ComandoDeProjeto, IRequest
{
    public long? ClienteId { get; set; }
}

public sealed class VincularClienteCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<VincularClienteCommand>
{
    public async Task Handle(VincularClienteCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        projeto.VincularCliente(request.ClienteId);
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}

public sealed class VincularClienteCommandValidator : ValidatorBase<VincularClienteCommand>
{
    public VincularClienteCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);
        RuleFor(c => c.ClienteId).MustExists<VincularClienteCommand, Cliente>(UnitOfWork).WithName("Cliente");
    }
}
