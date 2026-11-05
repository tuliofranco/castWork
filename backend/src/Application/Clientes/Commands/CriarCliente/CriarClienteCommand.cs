using Castwork.Application.Abstractions;
using Castwork.Application.Common.Enderecos;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Clientes;
using Castwork.Domain.ValueObjects;

using MediatR;

namespace Castwork.Application.Clientes.Commands.CriarCliente;

/// <summary>
/// Cadastra um cliente do ateliê. Só o nome é obrigatório: no balcão, muitas vezes o joalheiro
/// anota o nome e o telefone e completa o resto depois. Retorna o Id criado.
/// </summary>
[Permissao(Regras.Clientes.Criar)]
public sealed class CriarClienteCommand : IRequest<long>
{
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// CPF ou CNPJ, com ou sem máscara.
    /// </summary>
    public string? Documento { get; set; }

    public string? Email { get; set; }

    public string? Telefone { get; set; }

    public EnderecoDto? Endereco { get; set; }

    public string? Observacoes { get; set; }
}

public sealed class CriarClienteCommandHandler(IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IRequestHandler<CriarClienteCommand, long>
{
    public async Task<long> Handle(CriarClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = Cliente.Criar(
            request.Nome,
            string.IsNullOrWhiteSpace(request.Documento) ? null : Documento.Criar(request.Documento),
            request.Email,
            request.Telefone,
            request.Endereco?.ParaValueObject(),
            request.Observacoes,
            timeProvider.GetUtcNow());

        unitOfWork.GetRepository<Cliente>().Adicionar(cliente);
        await unitOfWork.SalvarAsync(cancellationToken);

        return cliente.Id;
    }
}
