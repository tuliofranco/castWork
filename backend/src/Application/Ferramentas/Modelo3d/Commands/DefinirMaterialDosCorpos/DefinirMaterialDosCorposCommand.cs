using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;

using MediatR;

namespace Castwork.Application.Ferramentas.Modelo3d.Commands.DefinirMaterialDosCorpos;

/// <summary>
/// Conferência de peso e materiais: define o material de um ou vários corpos do modelo 3D da versão (uma liga ativa do
/// ateliê, "Pedra" ou "Apoio (não conta)"). Aplica no arquivo atual com o <c>material.assign</c> do SolidCraft, sem IA, e
/// grava uma nova versão do arquivo (desfazível). Não cria nem apaga corpos. Só em versão em desenvolvimento.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class DefinirMaterialDosCorposCommand : ComandoDoModelo, IRequest<ModeloAlteradoDto>
{
    public const int CorposMaximo = 200;

    public IReadOnlyList<MaterialInformado> Corpos { get; set; } = [];
}

public sealed class MaterialInformado
{
    /// <summary>
    /// O nome do corpo no modelo.
    /// </summary>
    public string Corpo { get; set; } = string.Empty;

    /// <summary>
    /// O nome do material no arquivo: <c>Castwork liga {id}</c>, "Pedra" ou "Apoio (não conta)".
    /// </summary>
    public string Material { get; set; } = string.Empty;
}

public sealed class DefinirMaterialDosCorposCommandHandler(IUnitOfWork unitOfWork, IMedidorDoModelo medidor, IEditorDoModelo editor, TimeProvider relogio)
    : IRequestHandler<DefinirMaterialDosCorposCommand, ModeloAlteradoDto>
{
    public Task<ModeloAlteradoDto> Handle(DefinirMaterialDosCorposCommand request, CancellationToken cancellationToken)
        => AlteracaoNaConferencia.AplicarAsync(unitOfWork, medidor, editor, relogio, request, modelo => Montar(modelo.Medicao!, request.Corpos), cancellationToken);

    /// <summary>
    /// Os corpos precisam estar no modelo conferido (o material, o validador já garantiu que é do ateliê).
    /// </summary>
    internal static DefinicaoDeMateriais Montar(MedicaoDoModelo medicao, IReadOnlyList<MaterialInformado> corpos)
    {
        _ = AlteracaoNaConferencia.Corpos(medicao, corpos.Select(c => c.Corpo));
        return new DefinicaoDeMateriais([.. corpos.Select(c => new MaterialDoCorpo(c.Corpo, c.Material))]);
    }
}
