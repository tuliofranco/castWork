using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Application.Projetos.Commands;
using Castwork.Domain.Entities.Briefings;

using FluentValidation;

using MediatR;

namespace Castwork.Application.Briefings.Commands.SalvarFormularioBriefing;

/// <summary>
/// Cria o briefing do projeto com o formulário inicial, ou corrige o formulário de um briefing existente
/// (a IA vê a correção na próxima mensagem). Retorna o Id do briefing.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class SalvarFormularioBriefingCommand : ComandoDeProjeto, IRequest<long>
{
    public TipoJoia TipoJoia { get; set; }

    public string OQueOClienteQuer { get; set; } = string.Empty;

    public string? Ocasiao { get; set; }

    public string? Materiais { get; set; }

    public string? Medidas { get; set; }

    public DateOnly? Prazo { get; set; }

    public string? Orcamento { get; set; }
}

public sealed class SalvarFormularioBriefingCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<SalvarFormularioBriefingCommand, long>
{
    public async Task<long> Handle(SalvarFormularioBriefingCommand request, CancellationToken cancellationToken)
    {
        var formulario = new FormularioBriefing(request.TipoJoia, request.OQueOClienteQuer, request.Ocasiao, request.Materiais, request.Medidas, request.Prazo, request.Orcamento);
        var repositorio = unitOfWork.GetRepository<Briefing>();
        var existente = await repositorio.FindBy(b => b.ProjetoId == request.ProjetoId).Select(b => (long?)b.Id).SingleOrDefaultAsync(cancellationToken);

        Briefing briefing;
        if (existente is null)
        {
            briefing = Briefing.Criar(request.ProjetoId, formulario);
            repositorio.Adicionar(briefing);
        }
        else
        {
            briefing = await repositorio.ObterPorIdOuFalharAsync(existente.Value, cancellationToken);
            briefing.PreencherFormulario(formulario);
        }

        await unitOfWork.SalvarAsync(cancellationToken);
        return briefing.Id;
    }
}

public sealed class SalvarFormularioBriefingCommandValidator : ValidatorBase<SalvarFormularioBriefingCommand>
{
    public SalvarFormularioBriefingCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);
        RuleFor(c => c.TipoJoia).IsInEnum().WithName("Tipo de joia");
        RuleFor(c => c.OQueOClienteQuer).NotEmpty().MaximumLength(Briefing.TextoLongoMaxLength).WithName("O que o cliente quer");
        RuleFor(c => c.Ocasiao).MaximumLength(Briefing.TextoCurtoMaxLength).WithName("Ocasião");
        RuleFor(c => c.Materiais).MaximumLength(Briefing.TextoCurtoMaxLength);
        RuleFor(c => c.Medidas).MaximumLength(Briefing.TextoCurtoMaxLength);
        RuleFor(c => c.Orcamento).MaximumLength(Briefing.TextoCurtoMaxLength).WithName("Orçamento");
    }
}
