using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Atelie;

using MediatR;

namespace Castwork.Application.Atelie.Commands.DefinirLogo;

/// <summary>
/// Troca o logo do ateliê (PNG ou JPEG até 1 MB), ou remove com <see cref="Conteudo"/> nulo.
/// O tipo é conferido pelo conteúdo do arquivo no domínio, não pela extensão enviada.
/// </summary>
[Permissao(Regras.Atelie.Configurar)]
public sealed class DefinirLogoCommand : IRequest
{
    public byte[]? Conteudo { get; set; }
}

public sealed class DefinirLogoCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DefinirLogoCommand>
{
    public async Task Handle(DefinirLogoCommand request, CancellationToken cancellationToken)
    {
        var configuracao = await unitOfWork.GetRepository<ConfiguracaoAtelie>().ObterPorIdOuFalharAsync(ConfiguracaoAtelie.IdUnico, cancellationToken);
        if (request.Conteudo is null)
        {
            configuracao.RemoverLogo();
        }
        else
        {
            configuracao.DefinirLogo(request.Conteudo);
        }

        await unitOfWork.SalvarAsync(cancellationToken);
    }
}
