using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Exceptions;
using Castwork.DomainService.Precificacao;

using MediatR;

namespace Castwork.Application.Ferramentas.Modelo3d.Commands.UnirCorposSobrepostos;

/// <summary>
/// Conferência de peso e materiais: une corpos de metal da mesma liga que se sobrepõem, para a parte comum parar de contar
/// duas vezes no peso. Viram um corpo só, com o nome e o material do primeiro (<c>solid.combine</c> do SolidCraft, sem IA),
/// numa nova versão do arquivo (desfazível pelo histórico). Só em versão em desenvolvimento.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class UnirCorposSobrepostosCommand : ComandoDoModelo, IRequest<ModeloAlteradoDto>
{
    public const int CorposMaximo = 20;

    /// <summary>
    /// Os corpos a unir; o primeiro dá o nome ao corpo unido.
    /// </summary>
    public IReadOnlyList<string> Corpos { get; set; } = [];
}

public sealed class UnirCorposSobrepostosCommandHandler(IUnitOfWork unitOfWork, IMedidorDoModelo medidor, IEditorDoModelo editor, TimeProvider relogio)
    : IRequestHandler<UnirCorposSobrepostosCommand, ModeloAlteradoDto>
{
    public Task<ModeloAlteradoDto> Handle(UnirCorposSobrepostosCommand request, CancellationToken cancellationToken)
        => AlteracaoNaConferencia.AplicarAsync(unitOfWork, medidor, editor, relogio, request, modelo => Montar(modelo.Medicao!, request.Corpos), cancellationToken);

    /// <summary>
    /// Só metal da mesma liga (unir ouro amarelo com branco apagaria uma das ligas) e corpos que se sobrepõem entre si.
    /// </summary>
    internal static UniaoDeCorpos Montar(MedicaoDoModelo medicao, IReadOnlyList<string> nomes)
    {
        var corpos = AlteracaoNaConferencia.Corpos(medicao, nomes);
        if (corpos.Any(c => PesoDoModelo.Uso(c.Material) != UsoDoCorpo.Metal) || corpos.Select(c => c.Material).Distinct(StringComparer.Ordinal).Count() > 1)
        {
            throw new RegraDeNegocioException("Só dá para unir corpos de metal da mesma liga.");
        }

        if (!Conectados(nomes, medicao.Sobreposicoes))
        {
            throw new RegraDeNegocioException("Esses corpos não se sobrepõem: não há o que unir.");
        }

        return new UniaoDeCorpos(nomes[0], [.. nomes.Skip(1)]);
    }

    /// <summary>
    /// Todos ligados por sobreposições entre eles (a união dá um corpo só).
    /// </summary>
    private static bool Conectados(IReadOnlyList<string> nomes, IReadOnlyList<SobreposicaoMedida> sobreposicoes)
    {
        var pedidos = nomes.ToHashSet(StringComparer.Ordinal);
        var alcancados = new HashSet<string>(StringComparer.Ordinal) { nomes[0] };
        var fila = new Queue<string>([nomes[0]]);
        while (fila.TryDequeue(out var atual))
        {
            foreach (var s in sobreposicoes)
            {
                var outro = s.CorpoA == atual ? s.CorpoB : s.CorpoB == atual ? s.CorpoA : null;
                if (outro is not null && pedidos.Contains(outro) && alcancados.Add(outro))
                {
                    fila.Enqueue(outro);
                }
            }
        }

        return alcancados.Count == pedidos.Count;
    }
}
