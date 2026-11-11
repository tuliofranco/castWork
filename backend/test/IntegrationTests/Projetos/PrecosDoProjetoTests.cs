using Castwork.Application.Common.Exceptions;
using Castwork.Application.Common.Security;
using Castwork.Application.Precificacao.Commands.AtualizarCotacao;
using Castwork.Application.Precificacao.Commands.AtualizarParametrosDePreco;
using Castwork.Application.Precificacao.Queries.GetPrecosDoAtelie;
using Castwork.Application.Projetos.Commands.AlterarPrecosDoProjeto;
using Castwork.Application.Projetos.Commands.AtualizarPrecosDoProjeto;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using FluentValidation;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Castwork.IntegrationTests.Projetos;

/// <summary>
/// Preços do projeto pelo MediatR: herança na criação, edição só no projeto, atualizar para os valores atuais do
/// ateliê, permissões e o preenchimento dos projetos que já existiam (migration <c>PrecosDoProjeto</c>).
/// </summary>
public class PrecosDoProjetoTests : TesteDeIntegracao
{
    private const string MigrationAnterior = "20261110005840_CustoDasPecas";

    [Test]
    public async Task DeveCriarProjetoComOsPrecosDoAtelieEADataAsync()
    {
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Ouro, Valor = 600m });
        AmbienteDeTeste.Relogio.Avancar(TimeSpan.FromDays(1));
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Ouro, Valor = 640m });
        await AtualizarParametrosDoAtelieAsync(perda: 9m, valorHora: 90m);
        AmbienteDeTeste.Relogio.Avancar(TimeSpan.FromDays(3));

        var id = await Usar(new ProjetoBuilder()).BuildAsync();

        var projeto = await LerAsync(id);
        projeto.Cotacoes.Select(c => (c.Metal, c.Valor.Valor, c.CotadaEm)).Should()
            .Equal([(Metal.Ouro, 640m, RelogioDeTeste.Inicio.AddDays(1))], "só a vigente, e a prata ainda não foi cotada");
        (projeto.ParametrosDePreco.PerdaPercentual, projeto.ParametrosDePreco.ValorHora.Valor, projeto.ParametrosDePreco.Markup).Should().Be((9m, 90m, 1.8m));
        (projeto.PrecosHerdadosEm, projeto.PrecosAlteradosEm).Should().Be((RelogioDeTeste.Inicio.AddDays(4), (DateTimeOffset?)null));
    }

    [Test]
    public async Task DeveAlterarPrecosDeUmProjetoSemAfetarOutrosNemOAtelieAsync()
    {
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Ouro, Valor = 640m });
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Prata, Valor = 6.8m });
        await AtualizarParametrosDoAtelieAsync(perda: 8m, valorHora: 90m);
        var projetos = Usar(new ProjetoBuilder());
        var editado = await projetos.BuildAsync();
        var outro = await projetos.BuildAsync();
        AmbienteDeTeste.Relogio.Avancar(TimeSpan.FromDays(2));

        await AmbienteDeTeste.SendAsync(Alteracao(editado, (Metal.Ouro, 655m), (Metal.Prata, 6.8m)));

        var projeto = await LerAsync(editado);
        projeto.Cotacoes.OrderBy(c => c.Metal).Select(c => (c.Metal, c.Valor.Valor, c.CotadaEm)).Should().Equal(
            (Metal.Ouro, 655m, RelogioDeTeste.Inicio.AddDays(2)),
            (Metal.Prata, 6.8m, RelogioDeTeste.Inicio));
        (projeto.ParametrosDePreco.PerdaPercentual, projeto.ParametrosDePreco.Markup).Should().Be((10m, 2.5m));
        projeto.PrecosAlteradosEm.Should().Be(RelogioDeTeste.Inicio.AddDays(2));

        var intocado = await LerAsync(outro);
        intocado.Cotacoes.OrderBy(c => c.Metal).Select(c => c.Valor.Valor).Should().Equal(640m, 6.8m);
        (intocado.ParametrosDePreco.PerdaPercentual, intocado.PrecosAlteradosEm).Should().Be((8m, (DateTimeOffset?)null));

        var atelie = await AmbienteDeTeste.SendAsync(new GetPrecosDoAtelieQuery());
        atelie.Cotacoes.Select(c => c.Atual!.Valor).Should().Equal(640m, 6.8m);
        atelie.Parametros.PerdaPercentual.Should().Be(8m);
    }

    [Test]
    public async Task DeveAtualizarParaOsValoresAtuaisDoAtelieAsync()
    {
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Ouro, Valor = 640m });
        await AtualizarParametrosDoAtelieAsync(perda: 8m, valorHora: 90m);
        var id = await Usar(new ProjetoBuilder()).BuildAsync();
        await AmbienteDeTeste.SendAsync(Alteracao(id, (Metal.Ouro, 700m)));
        AmbienteDeTeste.Relogio.Avancar(TimeSpan.FromDays(10));
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Ouro, Valor = 655m });
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Prata, Valor = 7.1m });
        await AtualizarParametrosDoAtelieAsync(perda: 9.5m, valorHora: 110m);
        AmbienteDeTeste.Relogio.Avancar(TimeSpan.FromHours(1));

        await AmbienteDeTeste.SendAsync(new AtualizarPrecosDoProjetoCommand { ProjetoId = id });

        var projeto = await LerAsync(id);
        var hoje = RelogioDeTeste.Inicio.AddDays(10);
        projeto.Cotacoes.OrderBy(c => c.Metal).Select(c => (c.Metal, c.Valor.Valor, c.CotadaEm)).Should().Equal((Metal.Ouro, 655m, hoje), (Metal.Prata, 7.1m, hoje));
        (projeto.ParametrosDePreco.PerdaPercentual, projeto.ParametrosDePreco.ValorHora.Valor, projeto.ParametrosDePreco.Markup).Should().Be((9.5m, 110m, 1.8m));
        (projeto.PrecosHerdadosEm, projeto.PrecosAlteradosEm).Should().Be((hoje.AddHours(1), (DateTimeOffset?)null));
    }

    [Test]
    public async Task DeveRecusarPrecosInvalidosEProjetoInexistenteAsync()
    {
        var id = await Usar(new ProjetoBuilder()).BuildAsync();
        var invalidos = Alteracao(id, (Metal.Ouro, 0m), (Metal.Ouro, 640m));
        invalidos.ValorHora = 0m;

        var alterar = () => AmbienteDeTeste.SendAsync(invalidos);
        var inexistente = () => AmbienteDeTeste.SendAsync(new AtualizarPrecosDoProjetoCommand { ProjetoId = 999_999 });

        (await alterar.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName)
            .Should().BeEquivalentTo("valorHora", "cotacoes", "cotacoes[0].valor");
        (await inexistente.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().Equal("projetoId");
    }

    [Test]
    public async Task DeveExigirEditarPrecificacaoParaAlterarEAtualizarOsPrecosAsync()
    {
        var id = await Usar(new ProjetoBuilder()).BuildAsync();
        var semEditar = await Usar(new PerfilBuilder()).ComRegras(Regras.Projetos.Editar, Regras.Precificacao.Consultar).BuildAsync();
        var editor = await Usar(new PerfilBuilder()).ComRegras(Regras.Precificacao.Editar).BuildAsync();
        AmbienteDeTeste.Sessao.IsAdmin = false;

        AmbienteDeTeste.Sessao.PerfilId = semEditar;
        var alterar = () => AmbienteDeTeste.SendAsync(Alteracao(id, (Metal.Ouro, 640m)));
        var atualizar = () => AmbienteDeTeste.SendAsync(new AtualizarPrecosDoProjetoCommand { ProjetoId = id });
        await alterar.Should().ThrowAsync<ForbiddenAccessException>();
        await atualizar.Should().ThrowAsync<ForbiddenAccessException>();

        AmbienteDeTeste.Sessao.PerfilId = editor;
        await alterar.Should().NotThrowAsync();
        await atualizar.Should().NotThrowAsync();
    }

    [Test]
    public async Task DeveHerdarOsPrecosMesmoSemARegraDePrecificacaoAsync()
    {
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Prata, Valor = 6.8m });
        var criador = await Usar(new PerfilBuilder()).ComRegras(Regras.Projetos.Criar).BuildAsync();
        (AmbienteDeTeste.Sessao.IsAdmin, AmbienteDeTeste.Sessao.PerfilId) = (false, criador);

        var id = await Usar(new ProjetoBuilder()).BuildAsync();

        (await LerAsync(id)).Cotacoes.Select(c => c.Valor.Valor).Should().Equal(6.8m);
    }

    [Test]
    public async Task DevePreencherNaMigrationOsProjetosQueJaExistiamAsync()
    {
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Ouro, Valor = 600m });
        AmbienteDeTeste.Relogio.Avancar(TimeSpan.FromDays(1));
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Ouro, Valor = 640m });
        await AtualizarParametrosDoAtelieAsync(perda: 9m, valorHora: 95m);
        var antesDaMigration = DateTimeOffset.UtcNow.AddMinutes(-1);
        long id;

        // Volta o banco para antes desta etapa, grava um projeto "antigo" e aplica a migration de novo.
        await MigrarAsync(MigrationAnterior);
        try
        {
            // INSERT ... RETURNING não aceita composição: lê a lista e pega o único Id no cliente.
            id = (await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Database
                .SqlQuery<long>($"INSERT INTO projetos (nome, criado_em) VALUES ('Colar antigo', now()) RETURNING id")
                .ToListAsync())).Single();
        }
        finally
        {
            await MigrarAsync(null);
        }

        try
        {
            var projeto = await LerAsync(id);
            projeto.Cotacoes.Select(c => (c.Metal, c.Valor.Valor, c.CotadaEm)).Should().Equal((Metal.Ouro, 640m, RelogioDeTeste.Inicio.AddDays(1)));
            (projeto.ParametrosDePreco.PerdaPercentual, projeto.ParametrosDePreco.ValorHora.Valor, projeto.ParametrosDePreco.Markup)
                .Should().Be((9m, 95m, 1.8m));
            projeto.PrecosHerdadosEm.Should().BeAfter(antesDaMigration, "a data da herança é a da migration");
            projeto.PrecosAlteradosEm.Should().BeNull();
        }
        finally
        {
            await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Projetos.Where(p => p.Id == id).ExecuteDeleteAsync());
        }
    }

    private static Task MigrarAsync(string? alvo)
        => AmbienteDeTeste.ExecutarNoBancoAsync(db => db.GetService<IMigrator>().MigrateAsync(alvo));

    private static AlterarPrecosDoProjetoCommand Alteracao(long projetoId, params (Metal Metal, decimal Valor)[] cotacoes)
        => new()
        {
            ProjetoId = projetoId,
            Cotacoes = [.. cotacoes.Select(c => new CotacaoInformada { Metal = c.Metal, Valor = c.Valor })],
            PerdaPercentual = 10m,
            ValorHora = 100m,
            Markup = 2.5m,
            TaxasPercentual = 4m,
            ArredondarPara = 50m,
        };

    private static Task AtualizarParametrosDoAtelieAsync(decimal perda, decimal valorHora)
        => AmbienteDeTeste.SendAsync(new AtualizarParametrosDePrecoCommand { PerdaPercentual = perda, ValorHora = valorHora, Markup = 1.8m, TaxasPercentual = 0m, ArredondarPara = 10m });

    private static Task<Projeto> LerAsync(long id)
        => AmbienteDeTeste.ExecutarNoBancoAsync<Projeto>(db => db.Projetos.Include(p => p.Cotacoes).AsNoTracking().SingleAsync(p => p.Id == id));
}
