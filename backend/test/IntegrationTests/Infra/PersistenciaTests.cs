using Castwork.Application.Abstractions;
using Castwork.Application.Projetos.Commands.CriarVersao;
using Castwork.Domain.Entities.Projetos;
using Castwork.Infra.Data;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.IntegrationTests.Infra;

/// <summary>
/// Comportamentos que só o PostgreSQL real comprova: migration aplicada, auditoria, concorrência via xmin
/// e enum gravado como texto.
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
        var projetos = Usar(new ProjetoBuilder());
        var id = await projetos.SendAsync(await projetos.BuildCommandAsync());
        await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = id, Nome = "Base" });
        var peca = await Usar(new PecaBuilder()).BuildAsync();
        AmbienteDeTeste.Relogio.Avancar(TimeSpan.FromHours(2));
        AmbienteDeTeste.Sessao.UsuarioId = 7;

        await AlterarProjetoAsync(id, projeto => projeto.AdicionarItem(projeto.Versoes.Single().Id, peca, 3, null));

        var projeto = await LerProjetoAsync(id);
        projeto.Should().BeEquivalentTo(
            new
            {
                CriadoEm = RelogioDeTeste.Inicio,
                CriadoPor = SessaoDeTeste.UsuarioPadraoId,
                AlteradoEm = (DateTimeOffset?)RelogioDeTeste.Inicio.AddHours(2),
                AlteradoPor = (long?)7L,
            },
            o => o.ExcludingMissingMembers());
        projeto.Versoes.Single().Itens.Should().ContainSingle().Which.Quantidade.Should().Be(3);
    }

    [Test]
    public async Task DeveLancarErroDeConcorrenciaQuandoDuasEdicoesPartemDaMesmaVersaoAsync()
    {
        var projetos = Usar(new ProjetoBuilder());
        var id = await projetos.SendAsync(await projetos.BuildCommandAsync());

        var salvarConcorrente = () => AmbienteDeTeste.ExecutarNoEscopoAsync(async sp =>
        {
            var primeira = sp.GetRequiredService<ApplicationDbContext>();
            await using var outroEscopo = sp.CreateAsyncScope();
            var segunda = outroEscopo.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var projetoA = await primeira.Projetos.SingleAsync(p => p.Id == id);
            var projetoB = await segunda.Projetos.SingleAsync(p => p.Id == id);

            projetoA.Atualizar("Versão A", null, null);
            await primeira.SaveChangesAsync();

            projetoB.Atualizar("Versão B", null, null);
            return await segunda.SaveChangesAsync();
        });

        await salvarConcorrente.Should().ThrowAsync<DbUpdateConcurrencyException>();
        (await LerProjetoAsync(id)).Nome.Should().Be("Versão A");
    }

    [Test]
    public async Task DeveGravarEnumsComoTextoLegivelAsync()
    {
        var projetos = Usar(new ProjetoBuilder());
        var id = await projetos.SendAsync(await projetos.BuildCommandAsync());
        await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = id, Nome = "Base" });
        var peca = await Usar(new PecaBuilder()).CustomizeCommand(c => c.Tipo = Domain.Entities.Pecas.TipoPeca.Fecho).BuildAsync();

        var valores = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Database
            .SqlQuery<string>($"SELECT status AS \"Value\" FROM projeto_versoes WHERE projeto_id = {id} UNION ALL SELECT tipo FROM pecas WHERE id = {peca} ORDER BY 1")
            .ToListAsync());

        valores.Should().Equal("EmDesenvolvimento", "Fecho");
    }

    private static Task<int> AlterarProjetoAsync(long id, Action<Projeto> alterar)
        => AmbienteDeTeste.ExecutarNoEscopoAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            alterar(await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(id, CancellationToken.None));
            return await unitOfWork.SalvarAsync(CancellationToken.None);
        });

    private static Task<Projeto> LerProjetoAsync(long id)
        => AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Projetos.Include(p => p.Versoes).ThenInclude(v => v.Itens).AsNoTracking().SingleAsync(p => p.Id == id));
}
