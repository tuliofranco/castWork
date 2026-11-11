using Castwork.Application.Projetos.Commands.CriarPecaDoProjeto;
using Castwork.Application.Projetos.Commands.DefinirValorDaPecaDoProjeto;
using Castwork.Application.Projetos.Commands.PublicarPecaDoProjeto;
using Castwork.Domain.Entities.Pecas;
using Castwork.TestBootstrap.Ambiente;

using Microsoft.EntityFrameworkCore;

namespace Castwork.TestBootstrap.Builders;

/// <summary>
/// Peça criada no projeto informado (rascunho, sem valor), com nome e tipo aleatórios (metal com uma liga do seed, ou
/// pedra com material livre). <see cref="ComValor"/> define o
/// valor e <see cref="Publicada"/> a publica na biblioteca logo depois de criar.
/// </summary>
public sealed class PecaDoProjetoBuilder(long projetoId) : DataBuilderAsync<PecaDoProjetoBuilder, CriarPecaDoProjetoCommand>
{
    private static readonly (TipoPeca Tipo, string Nome, long? LigaId, string? Material)[] Modelos =
    [
        (TipoPeca.Elo, "Elo oval", 2, null),
        (TipoPeca.Fecho, "Fecho lagosta", 2, null),
        (TipoPeca.Pedra, "Turmalina", null, "Turmalina"),
        (TipoPeca.Pingente, "Pingente gota", 7, null),
    ];

    private readonly List<long> _publicadas = [];
    private decimal? _valor;
    private bool _publicar;

    /// <summary>
    /// Peças da biblioteca criadas por <see cref="Publicada"/>.
    /// </summary>
    public IReadOnlyList<long> PecasDaBiblioteca => _publicadas;

    public PecaDoProjetoBuilder ComValor(decimal? valor)
    {
        _valor = valor;
        return this;
    }

    public PecaDoProjetoBuilder Publicada()
    {
        _publicar = true;
        return this;
    }

    /// <summary>
    /// Cria a peça e aplica o valor e a publicação pedidos.
    /// </summary>
    public async Task<long> CriarAsync()
    {
        var id = await BuildAsync();
        if (_valor is not null)
        {
            await AmbienteDeTeste.SendAsync(new DefinirValorDaPecaDoProjetoCommand { ProjetoId = projetoId, PecaDoProjetoId = id, Valor = _valor });
        }

        if (_publicar)
        {
            _publicadas.Add(await AmbienteDeTeste.SendAsync(new PublicarPecaDoProjetoCommand { ProjetoId = projetoId, PecaDoProjetoId = id }));
        }

        return id;
    }

    protected override Task<CriarPecaDoProjetoCommand> CriarCommandPadraoAsync()
    {
        var modelo = Faker.PickRandom(Modelos);
        return Task.FromResult(new CriarPecaDoProjetoCommand
        {
            ProjetoId = projetoId,
            Nome = $"{modelo.Nome} {Faker.Random.AlphaNumeric(4)}",
            Tipo = modelo.Tipo,
            LigaId = modelo.LigaId,
            Material = modelo.Material,
            PesoGramas = Math.Round(Faker.Random.Decimal(0.1m, 5m), 3),
        });
    }

    /// <summary>
    /// Desfaz o uso das peças nas versões (o item é só a ligação) e remove as peças e as que foram publicadas.
    /// </summary>
    protected override async Task RemoverAsync(IReadOnlyCollection<long> ids)
    {
        await AmbienteDeTeste.ExecutarNoBancoAsync(context => context.Database.ExecuteSqlAsync(
            $"DELETE FROM projeto_versao_itens WHERE peca_do_projeto_id = ANY({ids.ToArray()})"));
        await AmbienteDeTeste.ExecutarNoBancoAsync(context => context.Database.ExecuteSqlAsync(
            $"DELETE FROM projeto_pecas WHERE id = ANY({ids.ToArray()})"));
        if (_publicadas.Count > 0)
        {
            await AmbienteDeTeste.ExecutarNoBancoAsync(context => context.Database.ExecuteSqlAsync(
                $"UPDATE projeto_pecas SET peca_id = NULL WHERE peca_id = ANY({_publicadas.ToArray()})"));
            await AmbienteDeTeste.ExecutarNoBancoAsync(context => context.Pecas.Where(p => _publicadas.Contains(p.Id)).ExecuteDeleteAsync());
            _publicadas.Clear();
        }
    }
}
