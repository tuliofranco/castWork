using Castwork.Application.Abstractions;
using Castwork.Application.Projetos.Commands;

using FluentValidation;

namespace Castwork.Application.Assistente.Commands;

public static class FerramentaValidatorExtensions
{
    /// <summary>
    /// Projeto existente e ferramenta conhecida pelo assistente desta instalação, chamada pelo escopo dela. Ferramenta do
    /// projeto (o briefing) só se altera pela rota do projeto: dentro de uma versão ela é só para ver. Ferramenta da versão
    /// (desenho 2D, modelo 3D) abre pela versão ou, se também for da peça (<see cref="IFerramentaComIa.DaPeca"/>), pela peça
    /// do projeto.
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
        validator.RuleFor(c => c.PecaDoProjetoId)
            .Equal(0)
            .When(c => catalogo.Conhece(c.Ferramenta) && (catalogo.Obter(c.Ferramenta).DoProjeto || !catalogo.Obter(c.Ferramenta).DaPeca))
            .WithMessage("Esta ferramenta não é da peça do projeto.");
        validator.RuleFor(c => c.VersaoId)
            .GreaterThan(0)
            .When(c => c.PecaDoProjetoId == 0 && catalogo.Conhece(c.Ferramenta) && !catalogo.Obter(c.Ferramenta).DoProjeto)
            .WithMessage("Esta ferramenta é da versão: abra pela versão.");
        validator.RuleFor(c => c.VersaoId)
            .Equal(0)
            .When(c => c.PecaDoProjetoId != 0)
            .WithMessage("A ferramenta é da versão ou da peça, não das duas.");
    }
}
