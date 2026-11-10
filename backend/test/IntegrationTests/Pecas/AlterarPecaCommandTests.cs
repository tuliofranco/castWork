using Castwork.Application.Common.Security;
using Castwork.Application.Pecas.Commands.AlterarPeca;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.ValueObjects;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Pecas;

/// <summary>
/// Custo da peça pelo MediatR: criar e alterar com custo, tirar o custo e o custo protegido pela regra de precificação.
/// </summary>
public class AlterarPecaCommandTests : TesteDeIntegracao
{
    [Test]
    public async Task DeveCriarPecaComCustoPorUnidadeAsync()
    {
        var pecas = Usar(new PecaBuilder());
        var command = await pecas.CustomizeCommand(c => (c.Nome, c.Tipo, c.Custo) = ("Turmalina 4 mm", TipoPeca.Pedra, 85.5m)).BuildCommandAsync();

        var id = await pecas.SendAsync(command);

        (await CustoAsync(id)).Should().Be(Dinheiro.Criar(85.5m));
    }

    [Test]
    public async Task DeveAlterarDadosECustoETirarOCustoAsync()
    {
        var id = await Usar(new PecaBuilder()).CustomizeCommand(c => c.Custo = null).BuildAsync();

        await AmbienteDeTeste.SendAsync(Alteracao(id, custo: 120m));
        var peca = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.AsNoTracking().SingleAsync(p => p.Id == id));
        await AmbienteDeTeste.SendAsync(Alteracao(id, custo: null));

        peca.Should().BeEquivalentTo(new
        {
            Nome = "Fecho lagosta grande",
            Tipo = TipoPeca.Fecho,
            Material = "Ouro amarelo",
            Teor = TeorMetal.Ouro18k,
            Peso = Peso.EmGramas(0.5m),
            Descricao = "Com trava dupla",
            Custo = Dinheiro.Criar(120m),
            AlteradoEm = RelogioDeTeste.Inicio,
        });
        (await CustoAsync(id)).Should().BeNull("custo nulo deixa a peça sem custo");
    }

    [Test]
    public async Task DeveRecusarCustoNegativoEPecaInexistenteOuDesativadaAsync()
    {
        var id = await Usar(new PecaBuilder()).BuildAsync();
        var desativada = await Usar(new PecaBuilder()).BuildAsync();
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.Where(p => p.Id == desativada).ExecuteUpdateAsync(s => s.SetProperty(p => p.Ativo, false)));

        var negativo = () => AmbienteDeTeste.SendAsync(Alteracao(id, custo: -1m));
        var centavos = () => AmbienteDeTeste.SendAsync(Alteracao(id, custo: 10.005m));
        var inexistente = () => AmbienteDeTeste.SendAsync(Alteracao(999_999, custo: 10m));
        var inativa = () => AmbienteDeTeste.SendAsync(Alteracao(desativada, custo: 10m));

        (await negativo.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().Equal("custo");
        (await centavos.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().Equal("custo");
        (await inexistente.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().Equal("pecaId");
        (await inativa.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().Equal("pecaId");
    }

    [Test]
    public async Task DeveIgnorarOCustoDeQuemNaoConsultaAPrecificacaoAsync()
    {
        var comCusto = await Usar(new PecaBuilder()).CustomizeCommand(c => c.Custo = 40m).BuildAsync();
        var perfil = await Usar(new PerfilBuilder()).ComRegras(Regras.Pecas.Consultar, Regras.Pecas.Criar, Regras.Pecas.Alterar).BuildAsync();
        (AmbienteDeTeste.Sessao.IsAdmin, AmbienteDeTeste.Sessao.PerfilId) = (false, perfil);

        var criada = await Usar(new PecaBuilder()).CustomizeCommand(c => c.Custo = 99m).BuildAsync();
        await AmbienteDeTeste.SendAsync(Alteracao(comCusto, custo: null));

        (await CustoAsync(criada)).Should().BeNull("quem não vê o custo não o grava");
        (await CustoAsync(comCusto)).Should().Be(Dinheiro.Criar(40m), "a alteração de quem não vê o custo mantém o atual");
        (await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.AsNoTracking().SingleAsync(p => p.Id == comCusto))).Nome
            .Should().Be("Fecho lagosta grande", "os outros dados foram alterados");
    }

    [Test]
    public async Task DeveGravarOCustoComAsRegrasDePecasEDePrecificacaoAsync()
    {
        var id = await Usar(new PecaBuilder()).BuildAsync();
        var perfil = await Usar(new PerfilBuilder()).ComRegras(Regras.Pecas.Alterar, Regras.Precificacao.Consultar).BuildAsync();
        (AmbienteDeTeste.Sessao.IsAdmin, AmbienteDeTeste.Sessao.PerfilId) = (false, perfil);

        await AmbienteDeTeste.SendAsync(Alteracao(id, custo: 15.9m));

        (await CustoAsync(id)).Should().Be(Dinheiro.Criar(15.9m));
    }

    private static AlterarPecaCommand Alteracao(long id, decimal? custo) => new()
    {
        PecaId = id,
        Nome = "Fecho lagosta grande",
        Tipo = TipoPeca.Fecho,
        Material = "Ouro amarelo",
        Teor = "750",
        PesoGramas = 0.5m,
        Descricao = "Com trava dupla",
        Custo = custo,
    };

    private static Task<Dinheiro?> CustoAsync(long id)
        => AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.AsNoTracking().Where(p => p.Id == id).Select(p => p.Custo).SingleAsync());
}
