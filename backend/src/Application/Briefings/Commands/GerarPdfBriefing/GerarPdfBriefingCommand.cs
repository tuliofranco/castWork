using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Application.Projetos.Commands;

using MediatR;

namespace Castwork.Application.Briefings.Commands.GerarPdfBriefing;

/// <summary>
/// Gera um novo PDF do briefing com os dados atuais. Os anteriores continuam guardados.
/// Retorna o Id do documento.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class GerarPdfBriefingCommand : ComandoDeProjeto, IRequest<long>;

public sealed class GerarPdfBriefingCommandHandler(IUnitOfWork unitOfWork, IGeradorDocumentoBriefing gerador, TimeProvider relogio)
    : IRequestHandler<GerarPdfBriefingCommand, long>
{
    public async Task<long> Handle(GerarPdfBriefingCommand request, CancellationToken cancellationToken)
    {
        var briefing = await unitOfWork.ObterDoProjetoAsync(request.ProjetoId, cancellationToken);
        var documento = await GeracaoDePdf.GerarAsync(unitOfWork, gerador, relogio, briefing, cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);
        return documento.Id;
    }
}

public sealed class GerarPdfBriefingCommandValidator : ValidatorBase<GerarPdfBriefingCommand>
{
    public GerarPdfBriefingCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
        => this.ValidarProjeto(UnitOfWork);
}
