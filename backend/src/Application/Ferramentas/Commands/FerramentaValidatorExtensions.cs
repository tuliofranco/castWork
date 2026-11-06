using Castwork.Application.Abstractions;
using Castwork.Application.Projetos.Commands;

using FluentValidation;

namespace Castwork.Application.Ferramentas.Commands;

public static class FerramentaValidatorExtensions
{
    /// <summary>
    /// Projeto existente e ferramenta conhecida pelo assistente desta instalação.
    /// </summary>
    public static void ValidarFerramenta<T>(this AbstractValidator<T> validator, IUnitOfWork unitOfWork, IAssistenteFerramenta assistente)
        where T : ComandoDeFerramenta
    {
        validator.ValidarProjeto(unitOfWork);
        validator.RuleFor(c => c.Ferramenta)
            .Must(f => assistente.Ferramentas.Contains(f))
            .WithMessage("Ferramenta desconhecida.");
    }
}
