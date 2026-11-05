using Castwork.Application.Abstractions;
using Castwork.Application.Common.Enderecos;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Atelie;
using Castwork.Domain.ValueObjects;

using MediatR;

namespace Castwork.Application.Atelie.Commands.AtualizarConfiguracaoAtelie;

[Permissao(Regras.Atelie.Configurar)]
public sealed class AtualizarConfiguracaoAtelieCommand : IRequest
{
    public string Nome { get; set; } = string.Empty;

    public string? Documento { get; set; }

    public string? Email { get; set; }

    public string? Telefone { get; set; }

    public EnderecoDto? Endereco { get; set; }

    public decimal ValorHoraPadrao { get; set; }
}

public sealed class AtualizarConfiguracaoAtelieCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<AtualizarConfiguracaoAtelieCommand>
{
    public async Task Handle(AtualizarConfiguracaoAtelieCommand request, CancellationToken cancellationToken)
    {
        var configuracao = await unitOfWork.GetRepository<ConfiguracaoAtelie>()
            .ObterPorIdOuFalharAsync(ConfiguracaoAtelie.IdUnico, cancellationToken);

        configuracao.Atualizar(
            request.Nome,
            string.IsNullOrWhiteSpace(request.Documento) ? null : Documento.Criar(request.Documento),
            request.Email,
            request.Telefone,
            request.Endereco?.ParaValueObject(),
            Dinheiro.Criar(request.ValorHoraPadrao));

        await unitOfWork.SalvarAsync(cancellationToken);
    }
}
