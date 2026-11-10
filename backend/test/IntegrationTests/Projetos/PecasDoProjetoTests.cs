using System.Net;
using System.Net.Http.Json;

using Castwork.Application.Acesso.Commands.Login;
using Castwork.Application.Common.Exceptions;
using Castwork.Application.Common.Security;
using Castwork.Application.Precificacao.Commands.AlterarLiga;
using Castwork.Application.Precificacao.Commands.CriarLiga;
using Castwork.Application.Precificacao.Commands.DesativarLiga;
using Castwork.Application.Projetos.Commands.AdicionarItem;
using Castwork.Application.Projetos.Commands.AlterarPecaDoProjeto;
using Castwork.Application.Projetos.Commands.CriarPecaDoProjeto;
using Castwork.Application.Projetos.Commands.CriarVersao;
using Castwork.Application.Projetos.Commands.DefinirValorDaPecaDoProjeto;
using Castwork.Application.Projetos.Commands.ImportarPecaDaBiblioteca;
using Castwork.Application.Projetos.Commands.PublicarPecaDoProjeto;
using Castwork.Application.Projetos.Queries.GetPrecoDaVersao;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;
using Castwork.DomainService.Precificacao;
using Castwork.IntegrationTests.GraphQl;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using FluentValidation;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Castwork.IntegrationTests.Projetos;

/// <summary>
/// Peças do projeto pelo MediatR e pelas rotas <c>api/projetos/{id}/pecas</c>: criar, importar, alterar (vira outra
/// peça), valor e publicar; o material pela liga (cópia do nome e do teor) ou livre na pedra; as permissões; e as
/// migrations <c>PecasDoProjeto</c> e <c>LigaDasPecas</c> com dados no formato antigo.
/// </summary>
public class PecasDoProjetoTests : TesteDeIntegracao
{
    private const string MigrationAnterior = "20261110014120_OrcamentoDaVersao";

    [Test]
    public async Task DeveCriarPecaUsarNaVersaoDefinirOValorEVerOPrecoAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var pecas = Usar(new PecaDoProjetoBuilder(projetoId));
        var command = await pecas.CustomizeCommand(c => (c.Nome, c.Tipo, c.LigaId, c.Material, c.PesoGramas) = ("Turmalina 4 mm", TipoPeca.Pedra, null, " Turmalina ", null)).BuildCommandAsync();
        var pecaId = await pecas.SendAsync(command);
        var versaoId = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Base" });
        await AmbienteDeTeste.SendAsync(new AdicionarItemCommand { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaId, Quantidade = 3 });

        var semValor = await AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = versaoId });
        await AmbienteDeTeste.SendAsync(new DefinirValorDaPecaDoProjetoCommand { ProjetoId = projetoId, PecaDoProjetoId = pecaId, Valor = 85.5m });
        var comValor = await AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = versaoId });

        (await LerAsync(projetoId)).Pecas.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Nome = "Turmalina 4 mm",
            Tipo = TipoPeca.Pedra,
            LigaId = (long?)null,
            Material = "Turmalina",
            Teor = (TeorMetal?)null,
            Status = StatusPecaDoProjeto.Rascunho,
            PecaId = (long?)null,
            Valor = (Dinheiro?)Dinheiro.Criar(85.5m),
            Ativo = true,
        });
        semValor.Calculo!.Pendencias.Should().ContainSingle(p => p.Tipo == TipoDePendencia.PecaSemValor)
            .Which.Should().BeEquivalentTo(new { PecaDoProjetoId = (long?)pecaId, Descricao = "Turmalina 4 mm está sem valor no projeto." });
        comValor.Calculo!.Composicao.Should().ContainSingle().Which.Should().BeEquivalentTo(new { PecaDoProjetoId = pecaId, CustoUnitario = (decimal?)85.5m, Custo = 256.5m });
    }

    [Test]
    public async Task DeveImportarAlterarEPublicarComoOutraPecaSemMudarAOriginalAsync()
    {
        var origem = await Usar(new ProjetoBuilder()).BuildAsync();
        var destino = await Usar(new ProjetoBuilder()).BuildAsync();
        var pecasDaOrigem = Usar(new PecaDoProjetoBuilder(origem).Publicada());
        await pecasDaOrigem.CustomizeCommand(c => (c.Nome, c.Tipo, c.LigaId, c.Material, c.PesoGramas) = ("Elo oval", TipoPeca.Elo, 2, null, 0.35m)).CriarAsync();
        var original = pecasDaOrigem.PecasDaBiblioteca.Single();
        await AmbienteDeTeste.SendAsync(new AlterarLigaCommand { LigaId = 2, Nome = "Ouro amarelo 750", Metal = Metal.Ouro, Teor = "750", Densidade = 15.5m });

        var importada = await AmbienteDeTeste.SendAsync(new ImportarPecaDaBibliotecaCommand { ProjetoId = destino, PecaId = original });
        var aposImportar = (await LerAsync(destino)).Pecas.Single();
        await AmbienteDeTeste.SendAsync(new AlterarPecaDoProjetoCommand
        {
            ProjetoId = destino,
            PecaDoProjetoId = importada,
            Nome = "Elo oval grande",
            Tipo = TipoPeca.Elo,
            LigaId = 2,
            PesoGramas = 0.5m,
        });
        var aposAlterar = (await LerAsync(destino)).Pecas.Single();
        var nova = await AmbienteDeTeste.SendAsync(new PublicarPecaDoProjetoCommand { ProjetoId = destino, PecaDoProjetoId = importada });

        aposImportar.Should().BeEquivalentTo(new { Nome = "Elo oval", Status = StatusPecaDoProjeto.Publicada, PecaId = (long?)original, Valor = (Dinheiro?)null });
        (aposImportar.LigaId, aposImportar.Material, aposImportar.Teor).Should().Be(
            (2, "Ouro 18k amarelo", TeorMetal.Ouro18k),
            "publicar levou a liga e a cópia para a biblioteca, e importar a trouxe de volta (a liga renomeada depois não muda)");
        aposAlterar.Should().BeEquivalentTo(new { Nome = "Elo oval grande", Status = StatusPecaDoProjeto.Rascunho, PecaId = (long?)null });
        nova.Should().NotBe(original);
        (await LerAsync(destino)).Pecas.Single().Should().BeEquivalentTo(new { Status = StatusPecaDoProjeto.Publicada, PecaId = (long?)nova });
        var biblioteca = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.AsNoTracking().Where(p => p.Id == original || p.Id == nova).OrderBy(p => p.Id).ToListAsync());
        biblioteca.Select(p => (p.Id, p.Nome, p.Peso, p.LigaId, p.Material)).Should().Equal(
            (original, "Elo oval", Peso.EmGramas(0.35m), 2, "Ouro 18k amarelo"),
            (nova, "Elo oval grande", Peso.EmGramas(0.5m), 2, "Ouro 18k amarelo"));
        biblioteca[1].CriadoEm.Should().Be(RelogioDeTeste.Inicio, "a peça nova da biblioteca é auditada como as outras");

        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Database.ExecuteSqlAsync($"UPDATE projeto_pecas SET peca_id = NULL WHERE peca_id = {nova}"));
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.Where(p => p.Id == nova).ExecuteDeleteAsync());
    }

    [Test]
    public async Task DeveRecusarImportarDeNovoPecaDesativadaEValorInvalidoAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var bibliotecas = Usar(new PecaBuilder());
        var naBiblioteca = await bibliotecas.BuildAsync();
        var desativada = await bibliotecas.BuildAsync();
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.Where(p => p.Id == desativada).ExecuteUpdateAsync(s => s.SetProperty(p => p.Ativo, false)));
        var importada = await AmbienteDeTeste.SendAsync(new ImportarPecaDaBibliotecaCommand { ProjetoId = projetoId, PecaId = naBiblioteca });

        var deNovo = () => AmbienteDeTeste.SendAsync(new ImportarPecaDaBibliotecaCommand { ProjetoId = projetoId, PecaId = naBiblioteca });
        var inativa = () => AmbienteDeTeste.SendAsync(new ImportarPecaDaBibliotecaCommand { ProjetoId = projetoId, PecaId = desativada });
        var negativo = () => AmbienteDeTeste.SendAsync(new DefinirValorDaPecaDoProjetoCommand { ProjetoId = projetoId, PecaDoProjetoId = importada, Valor = -1m });
        var centavos = () => AmbienteDeTeste.SendAsync(new DefinirValorDaPecaDoProjetoCommand { ProjetoId = projetoId, PecaDoProjetoId = importada, Valor = 10.005m });
        var inexistente = () => AmbienteDeTeste.SendAsync(new DefinirValorDaPecaDoProjetoCommand { ProjetoId = projetoId, PecaDoProjetoId = 999_999, Valor = 1m });
        var publicada = () => AmbienteDeTeste.SendAsync(new PublicarPecaDoProjetoCommand { ProjetoId = projetoId, PecaDoProjetoId = importada });
        var dadosInvalidos = () => AmbienteDeTeste.SendAsync(new CriarPecaDoProjetoCommand { ProjetoId = projetoId, Tipo = (TipoPeca)42, LigaId = 999_999 });

        await deNovo.Should().ThrowAsync<RegraDeNegocioException>().WithMessage("*já está neste projeto*");
        (await inativa.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().Equal("pecaId");
        (await negativo.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().Equal("valor");
        (await centavos.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().Equal("valor");
        await inexistente.Should().ThrowAsync<EntidadeNaoEncontradaException>();
        await publicada.Should().ThrowAsync<RegraDeNegocioException>().WithMessage("*já está publicada*");
        (await dadosInvalidos.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().BeEquivalentTo("nome", "tipo", "ligaId");
    }

    [Test]
    public async Task DeveCopiarONomeEOTeorDaLigaEscolhidaSemMudarQuandoALigaMudaAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var liga = await AmbienteDeTeste.SendAsync(new CriarLigaCommand { Nome = "Ouro 18k vermelho", Metal = Metal.Ouro, Teor = "18k", Densidade = 15.2m });
        var pecaId = await Usar(new PecaDoProjetoBuilder(projetoId))
            .CustomizeCommand(c => (c.Nome, c.Tipo, c.LigaId, c.Material) = ("Elo vermelho", TipoPeca.Elo, liga, null))
            .CriarAsync();

        await AmbienteDeTeste.SendAsync(new AlterarLigaCommand { LigaId = liga, Nome = "Ouro 14k vermelho", Metal = Metal.Ouro, Teor = "14k", Densidade = 13.2m });
        await AmbienteDeTeste.SendAsync(new DesativarLigaCommand { LigaId = liga });
        await AmbienteDeTeste.SendAsync(new AlterarPecaDoProjetoCommand { ProjetoId = projetoId, PecaDoProjetoId = pecaId, Nome = "Elo vermelho fino", Tipo = TipoPeca.Elo, LigaId = liga });

        var peca = (await LerAsync(projetoId)).Pecas.Single();
        (peca.Nome, peca.LigaId, peca.Material, peca.Teor).Should().Be(
            ("Elo vermelho fino", liga, "Ouro 18k vermelho", TeorMetal.Ouro18k),
            "a peça guarda a cópia de quando a liga foi escolhida, e salvar com a mesma liga (já desativada) a mantém");
    }

    [Test]
    public async Task DeveRecusarLigaDesativadaNaPecaNovaLigaNaPedraEMaterialLivreNoMetalAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var pecaId = await Usar(new PecaDoProjetoBuilder(projetoId)).CustomizeCommand(c => (c.Tipo, c.LigaId, c.Material) = (TipoPeca.Elo, 2, null)).CriarAsync();
        await AmbienteDeTeste.SendAsync(new DesativarLigaCommand { LigaId = 5 });

        var desativadaNaNova = () => AmbienteDeTeste.SendAsync(new CriarPecaDoProjetoCommand { ProjetoId = projetoId, Nome = "Elo", Tipo = TipoPeca.Elo, LigaId = 5 });
        var desativadaNaTroca = () => AmbienteDeTeste.SendAsync(new AlterarPecaDoProjetoCommand { ProjetoId = projetoId, PecaDoProjetoId = pecaId, Nome = "Elo", Tipo = TipoPeca.Elo, LigaId = 5 });
        var ligaNaPedra = () => AmbienteDeTeste.SendAsync(new CriarPecaDoProjetoCommand { ProjetoId = projetoId, Nome = "Turmalina", Tipo = TipoPeca.Pedra, LigaId = 2 });
        var textoNoMetal = () => AmbienteDeTeste.SendAsync(new CriarPecaDoProjetoCommand { ProjetoId = projetoId, Nome = "Elo", Tipo = TipoPeca.Elo, Material = "Ouro amarelo" });

        (await desativadaNaNova.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { PropertyName = "ligaId", ErrorMessage = "Liga não encontrada ou desativada." });
        await desativadaNaTroca.Should().ThrowAsync<RegraDeNegocioException>().WithMessage("*Ouro 14k amarelo está desativada*");
        (await ligaNaPedra.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().Equal("ligaId");
        (await textoNoMetal.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().Equal("material");
        (await LerAsync(projetoId)).Pecas.Single().LigaId.Should().Be(2);
    }

    [Test]
    public async Task DeveVirarOutraPecaAoTrocarALigaDeUmaPublicadaAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var pecas = Usar(new PecaDoProjetoBuilder(projetoId).Publicada());
        var pecaId = await pecas.CustomizeCommand(c => (c.Nome, c.Tipo, c.LigaId, c.Material, c.PesoGramas, c.Descricao) = ("Fecho", TipoPeca.Fecho, 2, null, 1.2m, null)).CriarAsync();
        var naBiblioteca = pecas.PecasDaBiblioteca.Single();

        await AmbienteDeTeste.SendAsync(new AlterarPecaDoProjetoCommand { ProjetoId = projetoId, PecaDoProjetoId = pecaId, Nome = "Fecho", Tipo = TipoPeca.Fecho, LigaId = 2, PesoGramas = 1.2m });
        var mesmaLiga = (await LerAsync(projetoId)).Pecas.Single();
        await AmbienteDeTeste.SendAsync(new AlterarPecaDoProjetoCommand { ProjetoId = projetoId, PecaDoProjetoId = pecaId, Nome = "Fecho", Tipo = TipoPeca.Fecho, LigaId = 3, PesoGramas = 1.2m });
        var outraLiga = (await LerAsync(projetoId)).Pecas.Single();

        mesmaLiga.Should().BeEquivalentTo(new { Status = StatusPecaDoProjeto.Publicada, PecaId = (long?)naBiblioteca });
        outraLiga.Should().BeEquivalentTo(new { Status = StatusPecaDoProjeto.Rascunho, PecaId = (long?)null, LigaId = (long?)3, Material = "Ouro 18k branco" });
        (await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.AsNoTracking().SingleAsync(p => p.Id == naBiblioteca)))
            .Should().BeEquivalentTo(new { LigaId = (long?)2, Material = "Ouro 18k amarelo" }, "a peça da biblioteca não muda");
    }

    [Test]
    public async Task DeveExigirEditarProjetosParaOsDadosEEditarPrecificacaoParaOValorAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var peca = await Usar(new PecaDoProjetoBuilder(projetoId)).CriarAsync();
        var outra = await Usar(new PecaDoProjetoBuilder(projetoId)).CriarAsync();
        var naBiblioteca = await Usar(new PecaBuilder()).BuildAsync();
        var editorDeProjetos = await Usar(new PerfilBuilder()).ComRegras(Regras.Projetos.Editar).BuildAsync();
        var editorDePrecos = await Usar(new PerfilBuilder()).ComRegras(Regras.Precificacao.Editar).BuildAsync();
        var dados = new Func<Task>[]
        {
            () => AmbienteDeTeste.SendAsync(new CriarPecaDoProjetoCommand { ProjetoId = projetoId, Nome = "Fecho", Tipo = TipoPeca.Fecho }),
            () => AmbienteDeTeste.SendAsync(new ImportarPecaDaBibliotecaCommand { ProjetoId = projetoId, PecaId = naBiblioteca }),
            () => AmbienteDeTeste.SendAsync(new AlterarPecaDoProjetoCommand { ProjetoId = projetoId, PecaDoProjetoId = peca, Nome = "Fecho", Tipo = TipoPeca.Fecho }),
            () => AmbienteDeTeste.SendAsync(new PublicarPecaDoProjetoCommand { ProjetoId = projetoId, PecaDoProjetoId = outra }),
        };
        var valor = () => AmbienteDeTeste.SendAsync(new DefinirValorDaPecaDoProjetoCommand { ProjetoId = projetoId, PecaDoProjetoId = peca, Valor = 10m });
        AmbienteDeTeste.Sessao.IsAdmin = false;

        AmbienteDeTeste.Sessao.PerfilId = editorDePrecos;
        foreach (var alterar in dados)
        {
            await alterar.Should().ThrowAsync<ForbiddenAccessException>();
        }

        await valor.Should().NotThrowAsync();

        AmbienteDeTeste.Sessao.PerfilId = editorDeProjetos;
        await valor.Should().ThrowAsync<ForbiddenAccessException>();
        foreach (var alterar in dados)
        {
            await alterar.Should().NotThrowAsync();
        }

        var publicadas = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Projetos.Where(p => p.Id == projetoId)
            .SelectMany(p => p.Pecas).Where(p => p.Id == outra).Select(p => p.PecaId!.Value).ToListAsync());
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Database.ExecuteSqlAsync($"UPDATE projeto_pecas SET peca_id = NULL WHERE id = {outra}"));
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.Where(p => publicadas.Contains(p.Id)).ExecuteDeleteAsync());
    }

    [Test]
    public async Task DeveResponderAsRotasDasPecasDoProjetoAsync()
    {
        using var http = AmbienteDeTeste.CriarClienteHttp();
        AmbienteDeTeste.Relogio.Agora = DateTimeOffset.UtcNow;
        var cliente = await EntrarAsync(http, Usar(new UsuarioBuilder()).Admin());
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var naBiblioteca = await Usar(new PecaBuilder()).BuildAsync();
        var rota = $"/api/projetos/{projetoId}/pecas";

        var criada = await SendAsync(http, cliente, HttpMethod.Post, rota, new { nome = "Elo torcido", tipo = "Elo", ligaId = 2 });
        var id = (await criada.Content.ReadFromJsonAsync<IdLido>())!.Id;
        var importada = await SendAsync(http, cliente, HttpMethod.Post, $"{rota}/importar", new { pecaId = naBiblioteca });
        var alterada = await SendAsync(http, cliente, HttpMethod.Put, $"{rota}/{id}", new { nome = "Elo torcido fino", tipo = "Elo", ligaId = 3, ativo = true });
        var valor = await SendAsync(http, cliente, HttpMethod.Put, $"{rota}/{id}/valor", new { valor = 12.5m });
        var publicada = await SendAsync(http, cliente, HttpMethod.Post, $"{rota}/{id}/publicar", null);
        var novaNaBiblioteca = (await publicada.Content.ReadFromJsonAsync<IdLido>())!.Id;
        var deNovo = await SendAsync(http, cliente, HttpMethod.Post, $"{rota}/{id}/publicar", null);
        var inexistente = await SendAsync(http, cliente, HttpMethod.Put, $"{rota}/999999/valor", new { valor = 1m });
        var lida = await cliente.ExecutarAsync(
            "query ($id: Long!) { projeto(id: $id) { pecas { id nome status pecaId valor ligaId material teor } } }",
            new { id = projetoId });

        (criada.StatusCode, importada.StatusCode, alterada.StatusCode, valor.StatusCode, publicada.StatusCode, deNovo.StatusCode, inexistente.StatusCode).Should().Be(
            (HttpStatusCode.Created, HttpStatusCode.Created, HttpStatusCode.NoContent, HttpStatusCode.NoContent, HttpStatusCode.Created, HttpStatusCode.UnprocessableEntity, HttpStatusCode.NotFound));
        lida["errors"].Should().BeNull();
        var pecaLida = lida["data"]!["projeto"]!["pecas"]!.AsArray().Single(p => p!["id"]!.GetValue<long>() == id)!;
        (pecaLida["nome"]!.GetValue<string>(), pecaLida["status"]!.GetValue<string>(), pecaLida["pecaId"]!.GetValue<long>(), pecaLida["valor"]!.GetValue<decimal>())
            .Should().Be(("Elo torcido fino", "PUBLICADA", novaNaBiblioteca, 12.5m));
        (pecaLida["ligaId"]!.GetValue<long>(), pecaLida["material"]!.GetValue<string>(), pecaLida["teor"]!.GetValue<int>()).Should().Be((3L, "Ouro 18k branco", 750));

        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Database.ExecuteSqlAsync($"UPDATE projeto_pecas SET peca_id = NULL WHERE peca_id = {novaNaBiblioteca}"));
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.Where(p => p.Id == novaNaBiblioteca).ExecuteDeleteAsync());
    }

    [Test]
    public async Task DeveListarAsLigasAtivasParaQuemConsultaProjetosSemPrecificacaoAsync()
    {
        using var http = AmbienteDeTeste.CriarClienteHttp();
        AmbienteDeTeste.Relogio.Agora = DateTimeOffset.UtcNow;
        var perfil = await Usar(new PerfilBuilder()).ComRegras(Regras.Projetos.Consultar).BuildAsync();
        var semRegra = await Usar(new PerfilBuilder()).ComRegras(Regras.Clientes.Consultar).BuildAsync();
        var leitor = await EntrarAsync(http, Usar(new UsuarioBuilder()).DoPerfil(perfil));
        var outro = await EntrarAsync(http, Usar(new UsuarioBuilder()).DoPerfil(semRegra));
        await AmbienteDeTeste.SendAsync(new DesativarLigaCommand { LigaId = 1 });

        var ligas = await leitor.ExecutarAsync("{ ligas { id nome teor } }");
        var negada = await outro.ExecutarAsync("{ ligas { id } }");

        ligas["errors"].Should().BeNull();
        ligas["data"]!["ligas"]!.AsArray().Select(l => l!["id"]!.GetValue<long>()).Should().BeEquivalentTo([2L, 3, 4, 5, 6, 7], "só as ativas");
        negada["errors"].Should().NotBeNull();
    }

    [Test]
    public async Task DeveCriarAsPecasDoProjetoNaMigrationPreservandoComposicoesECustosAsync()
    {
        long elo, fecho, semUso, colar, pulseira, colarV1, colarV2, pulseiraV1;

        // Volta o banco para antes desta etapa, grava peças com custo e composições no formato antigo e aplica de novo.
        await MigrarAsync(MigrationAnterior);
        try
        {
            elo = await InserirAsync($"INSERT INTO pecas (nome, tipo, material, teor, peso, ativo, custo, criado_em) VALUES ('Elo oval', 'Elo', 'Ouro amarelo', 750, 0.35, true, 120.00, now()) RETURNING id");
            fecho = await InserirAsync($"INSERT INTO pecas (nome, tipo, ativo, custo, criado_em) VALUES ('Fecho lagosta', 'Fecho', true, NULL, now()) RETURNING id");
            semUso = await InserirAsync($"INSERT INTO pecas (nome, tipo, ativo, custo, criado_em) VALUES ('Turmalina', 'Pedra', true, 50.00, now()) RETURNING id");
            colar = await InserirAsync($"INSERT INTO projetos (nome, criado_em) VALUES ('Colar antigo', now()) RETURNING id");
            pulseira = await InserirAsync($"INSERT INTO projetos (nome, criado_em) VALUES ('Pulseira antiga', now()) RETURNING id");
            colarV1 = await InserirAsync($"INSERT INTO projeto_versoes (projeto_id, numero, nome, status, publicada_em) VALUES ({colar}, 1, 'Base', 'Publicada', now()) RETURNING id");
            colarV2 = await InserirAsync($"INSERT INTO projeto_versoes (projeto_id, numero, nome, status) VALUES ({colar}, 2, 'Sem fecho', 'EmDesenvolvimento') RETURNING id");
            pulseiraV1 = await InserirAsync($"INSERT INTO projeto_versoes (projeto_id, numero, nome, status) VALUES ({pulseira}, 1, 'Base', 'EmDesenvolvimento') RETURNING id");
            await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Database.ExecuteSqlAsync($"""
                INSERT INTO projeto_versao_itens (versao_id, peca_id, quantidade, observacao) VALUES
                    ({colarV1}, {elo}, 12, 'ródio'), ({colarV1}, {fecho}, 1, NULL), ({colarV2}, {elo}, 14, NULL), ({pulseiraV1}, {elo}, 8, NULL)
                """));
        }
        finally
        {
            await MigrarAsync(null);
        }

        try
        {
            var doColar = await LerAsync(colar);
            var daPulseira = await LerAsync(pulseira);

            doColar.Pecas.OrderBy(p => p.Nome).Select(p => (p.Nome, p.Status, p.PecaId, p.Valor, p.Teor, p.Peso)).Should().Equal(
                ("Elo oval", StatusPecaDoProjeto.Publicada, (long?)elo, (Dinheiro?)Dinheiro.Criar(120m), (TeorMetal?)TeorMetal.Ouro18k, (Peso?)Peso.EmGramas(0.35m)),
                ("Fecho lagosta", StatusPecaDoProjeto.Publicada, (long?)fecho, (Dinheiro?)null, (TeorMetal?)null, (Peso?)null));
            daPulseira.Pecas.Select(p => (p.PecaId, p.Valor)).Should().Equal(((long?)elo, (Dinheiro?)Dinheiro.Criar(120m)));
            doColar.Pecas.Single(p => p.PecaId == elo).Should().BeEquivalentTo(
                new { LigaId = (long?)null, Material = "Ouro amarelo", Teor = (TeorMetal?)TeorMetal.Ouro18k },
                "LigaDasPecas: o material e o teor antigos ficam como cópia, sem adivinhar a liga");
            (await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.AsNoTracking().SingleAsync(p => p.Id == elo)))
                .Should().BeEquivalentTo(new { LigaId = (long?)null, Material = "Ouro amarelo", Teor = (TeorMetal?)TeorMetal.Ouro18k });
            var eloDoColar = doColar.Pecas.Single(p => p.PecaId == elo).Id;
            var fechoDoColar = doColar.Pecas.Single(p => p.PecaId == fecho).Id;
            doColar.Versoes.Single(v => v.Id == colarV1).Itens.OrderBy(i => i.Quantidade).Select(i => (i.PecaDoProjetoId, i.Quantidade, i.Observacao))
                .Should().Equal((fechoDoColar, 1, (string?)null), (eloDoColar, 12, "ródio"));
            doColar.Versoes.Single(v => v.Id == colarV2).Itens.Select(i => (i.PecaDoProjetoId, i.Quantidade)).Should().Equal([(eloDoColar, 14)], "a mesma peça do projeto nas versões");
            daPulseira.Versoes.Single().Itens.Single().PecaDoProjetoId.Should().Be(daPulseira.Pecas.Single().Id, "cada projeto tem a sua cópia");
            doColar.Versoes.Single(v => v.Id == colarV1).Status.Should().Be(StatusVersao.Publicada);
            (await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.AsNoTracking().Where(p => p.Id == elo || p.Id == fecho || p.Id == semUso).CountAsync()))
                .Should().Be(3, "as peças continuam na biblioteca, inclusive a que nenhum projeto usava");
            var preco = await AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = colarV2 });
            preco.Calculo!.CustoDaComposicao.Should().Be(1680m, "14 × R$ 120,00: o custo antigo virou o valor no projeto");
        }
        finally
        {
            await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Projetos.Where(p => p.Id == colar || p.Id == pulseira).ExecuteDeleteAsync());
            await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.Where(p => p.Id == elo || p.Id == fecho || p.Id == semUso).ExecuteDeleteAsync());
        }
    }

    private static Task MigrarAsync(string? alvo)
        => AmbienteDeTeste.ExecutarNoBancoAsync(db => db.GetService<IMigrator>().MigrateAsync(alvo));

    /// <summary>
    /// INSERT ... RETURNING não aceita composição: lê a lista e pega o único Id no cliente.
    /// </summary>
    private static async Task<long> InserirAsync(FormattableString sql)
        => (await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Database.SqlQuery<long>(sql).ToListAsync())).Single();

    private static Task<Projeto> LerAsync(long id)
        => AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Projetos
            .Include(p => p.Pecas)
            .Include(p => p.Versoes).ThenInclude(v => v.Itens)
            .AsSplitQuery()
            .AsNoTracking()
            .SingleAsync(p => p.Id == id));

    private static async Task<ClienteGraphQl> EntrarAsync(HttpClient http, UsuarioBuilder usuarios)
    {
        var command = await usuarios.BuildCommandAsync();
        await usuarios.SendAsync(command);
        var sessao = await AmbienteDeTeste.SendAsync(new LoginCommand { Email = command.Email, Senha = command.Senha });
        return new ClienteGraphQl(http, sessao.AccessToken);
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient http, ClienteGraphQl cliente, HttpMethod metodo, string url, object? corpo)
    {
        using var requisicao = new HttpRequestMessage(metodo, new Uri(url, UriKind.Relative)) { Content = corpo is null ? null : JsonContent.Create(corpo) };
        requisicao.Headers.Authorization = new("Bearer", cliente.Token);
        return await http.SendAsync(requisicao);
    }

    private sealed record IdLido(long Id);
}
