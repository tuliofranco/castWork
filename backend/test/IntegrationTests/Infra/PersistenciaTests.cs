using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Pecas;
using Castwork.Infra.Data;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.IntegrationTests.Infra;

/// <summary>
/// Comportamentos que só o PostgreSQL real comprova: migration aplicada, auditoria, concorrência via xmin
/// e enum gravado como código.
/// </summary>
public class PersistenciaTests : TesteDeIntegracao
{
    [Test]
    public async Task DeveTerAplicadoTodasAsMigrationsNaInicializacaoAsync()
    {
        var pendentes = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Database.GetPendingMigrationsAsync());

        pendentes.Should().BeEmpty();
    }

    [Test]
    public async Task DevePreencherAlteracaoSemMudarCriacaoQuandoSoUmFilhoDoAgregadoMudaAsync()
    {
        var id = await Usar(new PecaBuilder()).BuildAsync();
        AmbienteDeTeste.Relogio.Avancar(TimeSpan.FromHours(2));
        AmbienteDeTeste.Sessao.UsuarioId = 7;

        await AlterarPecaAsync(id, peca => peca.AdicionarObservacao("Cliente pediu aro 16", AmbienteDeTeste.Relogio.Agora));

        var peca = await LerPecaAsync(id);
        peca.Should().BeEquivalentTo(new
        {
            CriadoEm = RelogioDeTeste.Inicio,
            CriadoPor = SessaoDeTeste.UsuarioPadraoId,
            AlteradoEm = RelogioDeTeste.Inicio.AddHours(2),
            AlteradoPor = 7L,
        });
        peca.Observacoes.Should().ContainSingle().Which.Texto.Should().Be("Cliente pediu aro 16");
    }

    [Test]
    public async Task DeveLancarErroDeConcorrenciaQuandoDuasEdicoesPartemDaMesmaVersaoAsync()
    {
        var id = await Usar(new PecaBuilder()).BuildAsync();

        var salvarConcorrente = () => AmbienteDeTeste.ExecutarNoEscopoAsync(async sp =>
        {
            var primeira = sp.GetRequiredService<ApplicationDbContext>();
            await using var outroEscopo = sp.CreateAsyncScope();
            var segunda = outroEscopo.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var pecaA = await primeira.Pecas.SingleAsync(p => p.Id == id);
            var pecaB = await segunda.Pecas.SingleAsync(p => p.Id == id);

            pecaA.AtualizarDados("Versão A", null, null);
            await primeira.SaveChangesAsync();

            pecaB.AtualizarDados("Versão B", null, null);
            return await segunda.SaveChangesAsync();
        });

        await salvarConcorrente.Should().ThrowAsync<DbUpdateConcurrencyException>();
        (await LerPecaAsync(id)).Titulo.Should().Be("Versão A");
    }

    [Test]
    public async Task DeveGravarStatusDaPecaComoCodigoDeUmaLetraAsync()
    {
        var id = await Usar(new PecaBuilder()).BuildAsync();
        await AlterarPecaAsync(id, peca => peca.EnviarParaOrcamento(AmbienteDeTeste.Relogio.Agora));

        var codigos = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Database
            .SqlQuery<string>($"SELECT status AS \"Value\" FROM pecas WHERE id = {id} UNION ALL SELECT status FROM peca_etapas WHERE peca_id = {id} ORDER BY 1")
            .ToListAsync());

        codigos.Should().Equal("B", "O", "O");
    }

    private static Task<int> AlterarPecaAsync(long id, Action<Peca> alterar)
        => AmbienteDeTeste.ExecutarNoEscopoAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            alterar(await unitOfWork.GetRepository<Peca>().ObterPorIdOuFalharAsync(id, CancellationToken.None));
            return await unitOfWork.SalvarAsync(CancellationToken.None);
        });

    private static Task<Peca> LerPecaAsync(long id)
        => AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.Include(p => p.Observacoes).AsNoTracking().SingleAsync(p => p.Id == id));
}
