using Castwork.Application.Abstractions;
using Castwork.Application.Projetos.Commands;

using FluentValidation;

namespace Castwork.Application.Assistente.Commands;

public static class FerramentaValidatorExtensions
{
    /// <summary>
    /// Projeto existente e ferramenta conhecida pelo assistente desta instalação. Ferramenta do projeto (o
    /// briefing) só se altera pela rota do projeto: dentro de uma versão ela é só para ver.
    /// </summary>
    public static void ValidarFerramenta<T>(this AbstractValidator<T> validator, IUnitOfWork unitOfWork, CatalogoDeFerramentas catalogo)
        where T : ComandoDeFerramenta
    {
        validator.ValidarProjeto(unitOfWork);
        validator.RuleFor(c => c.Ferramenta)
            .Must(catalogo.Conhece)
            .WithMessage("Ferramenta desconhecida.");
        validator.RuleFor(c => c.VersaoId)
            .Equal(0)
            .When(c => catalogo.Conhece(c.Ferramenta) && catalogo.Obter(c.Ferramenta).DoProjeto)
            .WithMessage("Esta ferramenta é do projeto: edite pela página do projeto.");
        validator.RuleFor(c => c.VersaoId)
            .GreaterThan(0)
            .When(c => catalogo.Conhece(c.Ferramenta) && !catalogo.Obter(c.Ferramenta).DoProjeto)
            .WithMessage("Esta ferramenta é da versão: abra pela versão.");
    }
}
