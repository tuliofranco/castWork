using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Projetos;

using FluentValidation;

namespace Castwork.Application.Projetos.Commands;

public static class ProjetoValidatorExtensions
{
    /// <summary>
    /// Projeto informado na rota precisa existir. A existência da versão e do item fica com o agregado
    /// (lança não encontrado, 404), porque ele já carrega as versões.
    /// </summary>
    public static void ValidarProjeto<T>(this AbstractValidator<T> validator, IUnitOfWork unitOfWork)
        where T : ComandoDeProjeto
        => validator.RuleFor(c => c.ProjetoId)
            .GreaterThan(0)
            .MustExists<T, Projeto>(unitOfWork)
            .WithName("Projeto");
}
