using Castwork.Application.Common.Exceptions;
using Castwork.Application.Common.Security;
using Castwork.Application.Precificacao.Commands.AlterarLiga;
using Castwork.Application.Precificacao.Commands.AtualizarCotacao;
using Castwork.Application.Precificacao.Commands.AtualizarParametrosDePreco;
using Castwork.Application.Precificacao.Commands.CriarLiga;
using Castwork.Application.Precificacao.Commands.DesativarLiga;
using Castwork.Application.Precificacao.Queries.GetPrecosDoAtelie;
using Castwork.Domain.Entities.Precificacao;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Precificacao;

/// <summary>
/// Preços do ateliê pelo MediatR: seed da migration, histórico de cotações, CRUD de ligas, parâmetros e permissões.
/// </summary>
public class PrecosDoAtelieTests : TesteDeIntegracao
{
    [Test]
    public async Task DeveTerLigasPreCadastradasEParametrosPadraoSemCotacaoAsync()
    {
        var precos = await AmbienteDeTeste.SendAsync(new GetPrecosDoAtelieQuery());

        precos.Ligas.Select(l => (l.Nome, l.Teor, l.Densidade)).Should().Equal(
            ("Ouro 24k", 999, 19.3m),
            ("Ouro 18k amarelo", 750, 15.5m),
            ("Ouro 18k branco", 750, 15.0m),
            ("Ouro 18k rosé", 750, 15.1m),
            ("Ouro 14k amarelo", 585, 13.4m),
            ("Prata 950", 950, 10.4m),
            ("Prata 925", 925, 10.36m));
        precos.Parametros.Should().BeEquivalentTo(new ParametrosDePrecoDto { PerdaPercentual = 8m, ValorHora = 0m, Markup = 1.8m, TaxasPercentual = 0m, ArredondarPara = 10m });
        precos.Cotacoes.Should().BeEquivalentTo(new[] { new CotacaoVigenteDto { Metal = Metal.Ouro }, new CotacaoVigenteDto { Metal = Metal.Prata } });
    }

    [Test]
    public async Task DeveAtualizarCotacaoCriandoNovaLinhaSemApagarAAnteriorAsync()
    {
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Ouro, Valor = 600m });
        AmbienteDeTeste.Relogio.Avancar(TimeSpan.FromDays(3));
        AmbienteDeTeste.Sessao.UsuarioId = 42;
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Ouro, Valor = 612.35m });
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Prata, Valor = 7.1m });

        var precos = await AmbienteDeTeste.SendAsync(new GetPrecosDoAtelieQuery());
        var historico = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.CotacoesMetal.AsNoTracking()
            .Where(c => c.Metal == Metal.Ouro).OrderBy(c => c.RegistradaEm).Select(c => new { c.Valor, c.RegistradaEm, c.RegistradaPor }).ToListAsync());

        var ouro = precos.Cotacoes.Single(c => c.Metal == Metal.Ouro).Atual!;
        (ouro.Valor, ouro.RegistradaEm).Should().Be((612.35m, RelogioDeTeste.Inicio.AddDays(3)));
        precos.Cotacoes.Single(c => c.Metal == Metal.Prata).Atual!.Valor.Should().Be(7.1m);
        historico.Select(c => (c.Valor.Valor, c.RegistradaEm, c.RegistradaPor)).Should().Equal(
            (600m, RelogioDeTeste.Inicio, SessaoDeTeste.UsuarioPadraoId),
            (612.35m, RelogioDeTeste.Inicio.AddDays(3), 42L));
    }

    [TestCase(0)]
    [TestCase(-5)]
    public async Task DeveRecusarCotacaoZeroOuNegativaAsync(decimal valor)
    {
        var atualizar = () => AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Ouro, Valor = valor });

        (await atualizar.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().Equal("valor");
    }

    [Test]
    public async Task DeveCriarAlterarEDesativarLigasAsync()
    {
        var id = await AmbienteDeTeste.SendAsync(new CriarLigaCommand { Nome = "Ouro 18k vermelho", Metal = Metal.Ouro, Teor = "18k", Densidade = 15.2m });
        await AmbienteDeTeste.SendAsync(new AlterarLigaCommand { LigaId = 7, Nome = "Prata 925 (lei)", Metal = Metal.Prata, Teor = "925", Densidade = 10.4m });
        await AmbienteDeTeste.SendAsync(new DesativarLigaCommand { LigaId = 1 });

        var ligas = (await AmbienteDeTeste.SendAsync(new GetPrecosDoAtelieQuery())).Ligas;
        var desativada = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Ligas.AsNoTracking().SingleAsync(l => l.Id == 1));

        id.Should().BeGreaterThanOrEqualTo(1000, "as criadas pelo ateliê não colidem com os Ids do seed");
        ligas.Should().ContainEquivalentOf(new LigaDto { Id = id, Nome = "Ouro 18k vermelho", Metal = Metal.Ouro, Teor = 750, Densidade = 15.2m });
        ligas.Should().ContainEquivalentOf(new LigaDto { Id = 7, Nome = "Prata 925 (lei)", Metal = Metal.Prata, Teor = 925, Densidade = 10.4m });
        ligas.Select(l => l.Id).Should().NotContain(1);
        (desativada.Nome, desativada.Ativo).Should().Be(("Ouro 24k", false));
    }

    [Test]
    public async Task DeveRecusarLigaInvalidaRepetidaOuDesativadaAsync()
    {
        await AmbienteDeTeste.SendAsync(new DesativarLigaCommand { LigaId = 1 });

        var invalida = () => AmbienteDeTeste.SendAsync(new CriarLigaCommand { Nome = "prata 925", Metal = (Metal)42, Teor = "33k", Densidade = 0m });
        var desativada = () => AmbienteDeTeste.SendAsync(new AlterarLigaCommand { LigaId = 1, Nome = "Ouro 24k", Metal = Metal.Ouro, Teor = "999", Densidade = 19.3m });

        (await invalida.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName)
            .Should().BeEquivalentTo("nome", "metal", "teor", "densidade");
        (await desativada.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().Equal("ligaId");
        var nomeDaDesativada = await AmbienteDeTeste.SendAsync(new CriarLigaCommand { Nome = "Ouro 24k", Metal = Metal.Ouro, Teor = "999", Densidade = 19.3m });
        nomeDaDesativada.Should().BePositive("o nome de uma liga desativada fica livre");
    }

    [Test]
    public async Task DeveSalvarParametrosDePrecoAsync()
    {
        await AmbienteDeTeste.SendAsync(new AtualizarParametrosDePrecoCommand { PerdaPercentual = 10m, ValorHora = 85.5m, Markup = 2.2m, TaxasPercentual = 6.5m, ArredondarPara = 50m });

        var parametros = (await AmbienteDeTeste.SendAsync(new GetPrecosDoAtelieQuery())).Parametros;

        parametros.Should().BeEquivalentTo(new ParametrosDePrecoDto { PerdaPercentual = 10m, ValorHora = 85.5m, Markup = 2.2m, TaxasPercentual = 6.5m, ArredondarPara = 50m });
    }

    [Test]
    public async Task DeveRecusarParametrosForaDasFaixasAsync()
    {
        var salvar = () => AmbienteDeTeste.SendAsync(new AtualizarParametrosDePrecoCommand { PerdaPercentual = 60m, ValorHora = 0m, Markup = 0m, TaxasPercentual = -1m, ArredondarPara = -10m });

        (await salvar.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName)
            .Should().BeEquivalentTo("perdaPercentual", "valorHora", "markup", "taxasPercentual", "arredondarPara");
    }

    [Test]
    public async Task DevePermitirSoLeituraComConsultarENadaSemARegraAsync()
    {
        var leitor = await Usar(new PerfilBuilder()).ComRegras(Regras.Precificacao.Consultar).BuildAsync();
        var semRegra = await Usar(new PerfilBuilder()).ComRegras(Regras.Pecas.Consultar).BuildAsync();
        AmbienteDeTeste.Sessao.IsAdmin = false;

        AmbienteDeTeste.Sessao.PerfilId = leitor;
        var leitura = await AmbienteDeTeste.SendAsync(new GetPrecosDoAtelieQuery());
        var cotacao = () => AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Ouro, Valor = 600m });
        var liga = () => AmbienteDeTeste.SendAsync(new DesativarLigaCommand { LigaId = 1 });
        var parametros = () => AmbienteDeTeste.SendAsync(new AtualizarParametrosDePrecoCommand { PerdaPercentual = 8m, ValorHora = 50m, Markup = 1.8m, ArredondarPara = 10m });
        await cotacao.Should().ThrowAsync<ForbiddenAccessException>();
        await liga.Should().ThrowAsync<ForbiddenAccessException>();
        await parametros.Should().ThrowAsync<ForbiddenAccessException>();

        AmbienteDeTeste.Sessao.PerfilId = semRegra;
        var ler = () => AmbienteDeTeste.SendAsync(new GetPrecosDoAtelieQuery());
        await ler.Should().ThrowAsync<ForbiddenAccessException>();

        leitura.Ligas.Should().HaveCount(7);
    }

    [Test]
    public async Task DevePermitirAlteracaoComEditarAsync()
    {
        var editor = await Usar(new PerfilBuilder()).ComRegras(Regras.Precificacao.Consultar, Regras.Precificacao.Editar).BuildAsync();
        (AmbienteDeTeste.Sessao.IsAdmin, AmbienteDeTeste.Sessao.PerfilId) = (false, editor);

        var id = await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Prata, Valor = 7m });

        id.Should().BePositive();
    }
}
