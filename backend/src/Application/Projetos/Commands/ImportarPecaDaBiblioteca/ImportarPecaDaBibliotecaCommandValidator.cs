using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Pecas;

using FluentValidation;

namespace Castwork.Application.Projetos.Commands.ImportarPecaDaBiblioteca;

public sealed class ImportarPecaDaBibliotecaCommandValidator : ValidatorBase<ImportarPecaDaBibliotecaCommand>
{
    public ImportarPecaDaBibliotecaCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);

        // Só peça ativa da biblioteca.
        RuleFor(c => c.PecaId).GreaterThan(0).WithMessage("Informe a peça.").MustExists<ImportarPecaDaBibliotecaCommand, Peca>(UnitOfWork).WithName("Peça");
    }
}
