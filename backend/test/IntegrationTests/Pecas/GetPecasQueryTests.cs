using Castwork.Application.Pecas.Queries.GetPecas;
using Castwork.Domain.Entities.Pecas;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

namespace Castwork.IntegrationTests.Pecas;

public class GetPecasQueryTests : TesteDeIntegracao
{
    [Test]
    public async Task DeveListarPecasPorIdDecrescenteComNomeDoClienteAsync()
    {
        var clienteId = await Usar(new ClienteBuilder()).CustomizeCommand(c => c.Nome = "Maria Silva").BuildAsync();
        var pecas = Usar(new PecaBuilder()).DoCliente(clienteId);
        var anelId = await pecas.CustomizeCommand(c => c.Titulo = "Anel solitário").BuildAsync();
        AmbienteDeTeste.Relogio.Avancar(TimeSpan.FromDays(1));
        var brincoId = await pecas.CustomizeCommand(c => c.Titulo = "Brinco gota").BuildAsync();

        var resultado = await AmbienteDeTeste.SendAsync(new GetPecasQuery());

        resultado.Should().BeEquivalentTo(new { Pagina = 1, TotalRegistros = 2, TotalPaginas = 1 });
        resultado.Lista.Should().BeEquivalentTo(
            new[]
            {
                new { Id = brincoId, Titulo = "Brinco gota", Status = StatusPeca.Briefing, ClienteId = clienteId, ClienteNome = "Maria Silva", CriadoEm = RelogioDeTeste.Inicio.AddDays(1) },
                new { Id = anelId, Titulo = "Anel solitário", Status = StatusPeca.Briefing, ClienteId = clienteId, ClienteNome = "Maria Silva", CriadoEm = RelogioDeTeste.Inicio },
            },
            o => o.WithStrictOrdering());
    }

    [TestCase("ANEL", "Anel solitário")]
    [TestCase("joão", "Brinco gota")]
    [TestCase("SOUZA", "Brinco gota")]
    public async Task DeveFiltrarPecasPorTermoNoTituloOuNoNomeDoClienteSemDiferenciarMaiusculasAsync(string termo, string tituloEsperado)
    {
        var mariaId = await Usar(new ClienteBuilder()).CustomizeCommand(c => c.Nome = "Maria Silva").BuildAsync();
        var joaoId = await Usar(new ClienteBuilder()).CustomizeCommand(c => c.Nome = "João Souza").BuildAsync();
        await Usar(new PecaBuilder()).DoCliente(mariaId).CustomizeCommand(c => c.Titulo = "Anel solitário").BuildAsync();
        await Usar(new PecaBuilder()).DoCliente(joaoId).CustomizeCommand(c => c.Titulo = "Brinco gota").BuildAsync();

        var resultado = await AmbienteDeTeste.SendAsync(new GetPecasQuery { Termo = termo });

        resultado.Lista.Should().ContainSingle().Which.Titulo.Should().Be(tituloEsperado);
    }

    [Test]
    public async Task DeveFiltrarPecasPorClienteAsync()
    {
        var mariaId = await Usar(new ClienteBuilder()).BuildAsync();
        var joaoId = await Usar(new ClienteBuilder()).BuildAsync();
        var pecas = Usar(new PecaBuilder());
        var primeira = await pecas.DoCliente(mariaId).BuildAsync();
        var segunda = await pecas.DoCliente(mariaId).BuildAsync();
        await Usar(new PecaBuilder()).DoCliente(joaoId).BuildAsync();

        var resultado = await AmbienteDeTeste.SendAsync(new GetPecasQuery { ClienteId = mariaId });

        resultado.Lista.Select(p => (p.Id, p.ClienteId)).Should().Equal((segunda, mariaId), (primeira, mariaId));
    }

    [Test]
    public async Task DevePaginarEOrdenarPeloCampoPedidoComDesempatePorIdAsync()
    {
        var pecas = Usar(new PecaBuilder());
        var b1 = await pecas.CustomizeCommand(c => c.Titulo = "Brinco").BuildAsync();
        var a1 = await pecas.CustomizeCommand(c => c.Titulo = "Anel").BuildAsync();
        var a2 = await pecas.CustomizeCommand(c => c.Titulo = "Anel").BuildAsync();

        var primeira = await AmbienteDeTeste.SendAsync(new GetPecasQuery { OrdenarPor = "titulo", Decrescente = false, TamanhoPagina = 2 });
        var segunda = await AmbienteDeTeste.SendAsync(new GetPecasQuery { OrdenarPor = "titulo", Decrescente = false, TamanhoPagina = 2, Pagina = 2 });

        primeira.Lista.Select(p => (p.Titulo, p.Id)).Should().Equal(("Anel", a2), ("Anel", a1));
        segunda.Lista.Select(p => (p.Titulo, p.Id)).Should().Equal(("Brinco", b1));
        segunda.Should().BeEquivalentTo(new { TotalRegistros = 3, TotalPaginas = 2 });
    }
}
