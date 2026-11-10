using System.Net;
using System.Net.Http.Json;
using System.Text;

using Castwork.Application.Acesso.Commands.Login;
using Castwork.Application.Assistente.Commands;
using Castwork.Application.Assistente.Queries;
using Castwork.Application.Ferramentas.Modelo3d;
using Castwork.Application.Ferramentas.Modelo3d.Commands.MontarAPartirDasPecas;
using Castwork.Application.Projetos.Commands.AdicionarItem;
using Castwork.Application.Projetos.Commands.CriarVersao;
using Castwork.Application.Projetos.Commands.ImportarPecaDaBiblioteca;
using Castwork.Application.Projetos.Commands.PublicarPecaDoProjeto;
using Castwork.Application.Projetos.Commands.PublicarVersao;
using Castwork.Application.Projetos.Queries.GetPesoDaPeca;
using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;
using Castwork.IntegrationTests.GraphQl;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using FluentValidation;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.IntegrationTests.Projetos;

/// <summary>
/// O desenho 2D e o modelo 3D de cada peça do projeto: conversa e arquivo próprios (rota da peça), o arquivo final (trocar
/// o final de uma peça publicada faz dela outra peça; salvar não), publicar leva o final para a biblioteca e importar traz
/// uma cópia já final, o peso da peça pelo modelo 3D final (na liga dela), o "Montar a partir das peças" da versão e as
/// migrations <c>DesenhoDasPecas</c> e <c>ArquivoFinalDasPecas</c> com dados no formato antigo.
/// </summary>
public class DesenhoDasPecasTests : TesteDeIntegracao
{
    private const string Desenho = "desenho-2d";
    private const string Modelo = "modelo-3d";
    private const string MigrationAnterior = "20261110173452_LigaDasPecas";

    [Test]
    public async Task DeveTerConversaEArquivoDaPecaSeparadosDaVersaoEDasOutrasPecasAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var versaoId = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Base" });
        var pecas = Usar(new PecaDoProjetoBuilder(projetoId));
        var elo = await pecas.CustomizeCommand(c => (c.Nome, c.Tipo, c.LigaId, c.Material) = ("Elo oval", TipoPeca.Elo, 2, null)).BuildAsync();
        var fecho = await Usar(new PecaDoProjetoBuilder(projetoId)).BuildAsync();

        await SalvarAsync(projetoId, Desenho, "DXF do elo", pecaId: elo);
        await SalvarAsync(projetoId, Desenho, "DXF da versão", versaoId: versaoId);
        await AmbienteDeTeste.SendAsync(new ConversarFerramentaCommand { ProjetoId = projetoId, PecaDoProjetoId = elo, Ferramenta = Desenho, Texto = "Desenhe o elo" });

        (await ConteudoAsync(projetoId, Desenho, pecaId: elo)).Should().Be("DXF:Desenhe o elo", "a IA trabalhou no arquivo da peça");
        (await ConteudoAsync(projetoId, Desenho, versaoId: versaoId)).Should().Be("DXF da versão");
        (await ConteudoAsync(projetoId, Desenho, pecaId: fecho)).Should().BeNull("cada peça tem o seu desenho");
        var pedido = AssistenteFerramentaDeTeste.UltimoPedido!;
        Encoding.UTF8.GetString(pedido.Arquivo!).Should().Be("DXF do elo");
        pedido.Contexto.Should().Contain("\"peca\":").And.Contain("Elo oval").And.Contain("Ouro 18k amarelo").And.NotContain("\"versao\":");
        pedido.EscopoDaVersao.Should().BeFalse("na peça, o 3D continua livre");
        await AmbienteDeTeste.SendAsync(new ConversarFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Modelo, Texto = "Monte o colar" });
        AssistenteFerramentaDeTeste.UltimoPedido!.EscopoDaVersao.Should().BeTrue("na versão, a IA só monta e posiciona as peças");
        (await AmbienteDeTeste.SendAsync(new GetConversaFerramentaQuery { ProjetoId = projetoId, PecaDoProjetoId = fecho, Ferramenta = Desenho })).Mensagens.Should().BeEmpty();
        (await AmbienteDeTeste.SendAsync(new GetConversaFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho })).Mensagens.Should().BeEmpty();
    }

    [Test]
    public async Task DeveFazerDaPecaPublicadaOutraPecaSoAoTrocarOArquivoFinalAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var publicadas = Usar(new PecaDoProjetoBuilder(projetoId).Publicada());
        var marcada = await publicadas.CriarAsync();
        var soSalva = await publicadas.CriarAsync();
        var rascunho = await Usar(new PecaDoProjetoBuilder(projetoId)).BuildAsync();

        var arquivo = await SalvarAsync(projetoId, Modelo, """{"format":"solidcraft/51"}""", pecaId: marcada);
        await SalvarAsync(projetoId, Desenho, "DXF", pecaId: soSalva);
        await AmbienteDeTeste.SendAsync(new ConversarFerramentaCommand { ProjetoId = projetoId, PecaDoProjetoId = soSalva, Ferramenta = Desenho, Texto = "Desenhe o fecho" });
        var antesDeMarcar = (await LerAsync(projetoId)).Pecas.Single(p => p.Id == marcada).Status;
        var final = await FinalAsync(projetoId, Modelo, arquivo, marcada);
        var deNovo = await FinalAsync(projetoId, Modelo, arquivo, marcada);
        await SalvarFinalAsync(projetoId, Desenho, "DXF", rascunho);

        antesDeMarcar.Should().Be(StatusPecaDoProjeto.Publicada, "salvar um arquivo novo sem marcá-lo não muda o desenho");
        final.Should().Be(new ArquivoFinalDto(arquivo, Ajustado: false, VirouOutraPeca: true));
        deNovo.VirouOutraPeca.Should().BeFalse("o final já era este");
        var projeto = await LerAsync(projetoId);
        projeto.Pecas.Single(p => p.Id == marcada).Should().BeEquivalentTo(new { Status = StatusPecaDoProjeto.Rascunho, PecaId = (long?)null }, "trocar o arquivo final é mudar o desenho");
        projeto.Pecas.Single(p => p.Id == soSalva).Status.Should().Be(StatusPecaDoProjeto.Publicada, "a IA e o joalheiro só salvaram, sem marcar como final");
        projeto.Pecas.Single(p => p.Id == rascunho).Status.Should().Be(StatusPecaDoProjeto.Rascunho);
        (await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.CountAsync(p => publicadas.PecasDaBiblioteca.Contains(p.Id))))
            .Should().Be(2, "as peças da biblioteca não mudam");
    }

    [Test]
    public async Task DeveRecusarArquivarOFinalNovoChatEFinalForaDaPecaAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var versaoId = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Base" });
        var id = await Usar(new PecaDoProjetoBuilder(projetoId)).BuildAsync();
        var final = await SalvarFinalAsync(projetoId, Desenho, "DXF 1", id);
        var outro = await SalvarAsync(projetoId, Desenho, "DXF 2", pecaId: id);
        var daVersao = await SalvarAsync(projetoId, Desenho, "DXF", versaoId: versaoId);
        await AmbienteDeTeste.SendAsync(new ConversarFerramentaCommand { ProjetoId = projetoId, PecaDoProjetoId = id, Ferramenta = Desenho, Texto = "Oi" });

        var arquivarOFinal = () => AmbienteDeTeste.SendAsync(new ArquivarArquivoFerramentaCommand { ProjetoId = projetoId, PecaDoProjetoId = id, Ferramenta = Desenho, ArquivoId = final });
        var novoChat = () => AmbienteDeTeste.SendAsync(new NovoChatFerramentaCommand { ProjetoId = projetoId, PecaDoProjetoId = id, Ferramenta = Desenho });
        var finalNaVersao = () => AmbienteDeTeste.SendAsync(new MarcarArquivoFinalFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho, ArquivoId = daVersao });
        await AmbienteDeTeste.SendAsync(new ArquivarArquivoFerramentaCommand { ProjetoId = projetoId, PecaDoProjetoId = id, Ferramenta = Desenho, ArquivoId = outro });

        (await arquivarOFinal.Should().ThrowAsync<RegraDeNegocioException>()).WithMessage("*arquivo final da peça*");
        (await novoChat.Should().ThrowAsync<RegraDeNegocioException>()).WithMessage("*uma conversa só*");
        (await finalNaVersao.Should().ThrowAsync<ValidationException>()).WithMessage("*peça do projeto*");
        var finalLido = await AmbienteDeTeste.SendAsync(new GetArquivoFerramentaQuery { ProjetoId = projetoId, PecaDoProjetoId = id, Ferramenta = Desenho, Final = true });
        Encoding.UTF8.GetString(finalLido!.Conteudo).Should().Be("DXF 1", "arquivar outro arquivo não muda o final");
    }

    [Test]
    public async Task DeveMostrarNoVisualizadorOFinalENaoOAtualAsync()
    {
        using var http = AmbienteDeTeste.CriarClienteHttp();
        AmbienteDeTeste.Relogio.Agora = DateTimeOffset.UtcNow;
        var cliente = await EntrarAsync(http, Usar(new UsuarioBuilder()).Admin());
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var id = await Usar(new PecaDoProjetoBuilder(projetoId)).BuildAsync();
        var semFinal = await Usar(new PecaDoProjetoBuilder(projetoId)).BuildAsync();
        var final = await SalvarFinalAsync(projetoId, Modelo, "3D final", id);
        await SalvarAsync(projetoId, Modelo, "3D depois", pecaId: id);
        await SalvarAsync(projetoId, Modelo, "3D sem final", pecaId: semFinal);

        var doFinal = await AmbienteDeTeste.SendAsync(new GetArquivoFerramentaQuery { ProjetoId = projetoId, PecaDoProjetoId = id, Ferramenta = Modelo, Final = true });
        var nenhum = await AmbienteDeTeste.SendAsync(new GetArquivoFerramentaQuery { ProjetoId = projetoId, PecaDoProjetoId = semFinal, Ferramenta = Modelo, Final = true });
        using var pelaRota = await SendAsync(http, cliente, HttpMethod.Get, $"/api/projetos/{projetoId}/pecas/{id}/ferramentas/{Modelo}/arquivo?final=true");
        var lida = await cliente.ExecutarAsync(
            "query ($id: Long!) { projeto(id: $id) { pecas { id arquivosFinais { ferramenta arquivoId } } } }",
            new { id = projetoId });

        Encoding.UTF8.GetString(doFinal!.Conteudo).Should().Be("3D final");
        (await ConteudoAsync(projetoId, Modelo, pecaId: id)).Should().Be("3D depois", "o atual continua sendo o mais recente");
        nenhum.Should().BeNull("sem arquivo final, a peça não tem desenho");
        (await pelaRota.Content.ReadAsStringAsync()).Should().Be("3D final");
        lida["errors"].Should().BeNull();
        var finais = lida["data"]!["projeto"]!["pecas"]!.AsArray().ToDictionary(p => p!["id"]!.GetValue<long>(), p => p!["arquivosFinais"]!.AsArray());
        finais[id].Select(f => (f!["ferramenta"]!.GetValue<string>(), f["arquivoId"]!.GetValue<long>())).Should().Equal((Modelo, final));
        finais[semFinal].Should().BeEmpty();
    }

    [Test]
    public async Task DeveDarAosCorposONomeDaPecaAoMarcarOModeloFinalAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var id = await Usar(new PecaDoProjetoBuilder(projetoId)).CustomizeCommand(c => c.Nome = "Elo Infinito 2,5 cm").BuildAsync();
        var arquivo = await SalvarAsync(projetoId, Modelo, "3D do elo", pecaId: id);
        AmbienteDeTeste.Medidor.Medicao = new MedicaoDoModelo(
            [new CorpoMedido("Corpo1", null, 1m, 1m, Fechado: true), new CorpoMedido("Corpo2", null, 1m, 1m, Fechado: true)],
            []);

        var final = await FinalAsync(projetoId, Modelo, arquivo, id);

        final.Ajustado.Should().BeTrue();
        final.ArquivoId.Should().BeGreaterThan(arquivo, "o ajustado vira uma nova versão do arquivo");
        AmbienteDeTeste.Editor.Chamadas.Should().ContainSingle().Which.Alteracao.Should().BeEquivalentTo(
            new RenomeacaoDeCorpos([("Corpo1", "Elo Infinito 2,5 cm"), ("Corpo2", "Elo Infinito 2,5 cm (2)")]));
        var historico = await AmbienteDeTeste.SendAsync(new GetHistoricoArquivoFerramentaQuery { ProjetoId = projetoId, PecaDoProjetoId = id, Ferramenta = Modelo });
        historico[0].Should().BeEquivalentTo(new { Id = final.ArquivoId, Origem = OrigemArquivo.NomesDaPeca });
    }

    [Test]
    public async Task DeveRecusarFerramentaForaDoEscopoEPecaDeOutroProjetoAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var outroProjeto = await Usar(new ProjetoBuilder()).BuildAsync();
        var versaoId = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Base" });
        var daOutro = await Usar(new PecaDoProjetoBuilder(outroProjeto)).BuildAsync();
        var peca = await Usar(new PecaDoProjetoBuilder(projetoId)).BuildAsync();

        var briefingNaPeca = () => SalvarAsync(projetoId, "briefing", "x", pecaId: peca);
        var versaoEPeca = () => AmbienteDeTeste.SendAsync(new SalvarArquivoFerramentaCommand
        {
            ProjetoId = projetoId,
            VersaoId = versaoId,
            PecaDoProjetoId = peca,
            Ferramenta = Desenho,
            Conteudo = Convert.ToBase64String("x"u8.ToArray()),
        });
        var pecaDeOutroProjeto = () => SalvarAsync(projetoId, Desenho, "x", pecaId: daOutro);

        (await briefingNaPeca.Should().ThrowAsync<ValidationException>()).WithMessage("*não é da peça do projeto*");
        (await versaoEPeca.Should().ThrowAsync<ValidationException>()).WithMessage("*da versão ou da peça, não das duas*");
        await pecaDeOutroProjeto.Should().ThrowAsync<EntidadeNaoEncontradaException>();
    }

    [Test]
    public async Task DevePublicarLevandoODesenhoFinalEImportarUmaCopiaAsync()
    {
        using var http = AmbienteDeTeste.CriarClienteHttp();
        AmbienteDeTeste.Relogio.Agora = DateTimeOffset.UtcNow;
        var cliente = await EntrarAsync(http, Usar(new UsuarioBuilder()).Admin());
        var origem = await Usar(new ProjetoBuilder()).BuildAsync();
        var destino = await Usar(new ProjetoBuilder()).BuildAsync();
        var pecaId = await Usar(new PecaDoProjetoBuilder(origem)).BuildAsync();
        await SalvarAsync(origem, Desenho, "DXF antigo", pecaId: pecaId);
        await SalvarFinalAsync(origem, Desenho, "DXF final", pecaId);
        await SalvarAsync(origem, Desenho, "DXF depois do final", pecaId: pecaId);
        await SalvarFinalAsync(origem, Modelo, "3D final", pecaId);

        var naBiblioteca = await AmbienteDeTeste.SendAsync(new PublicarPecaDoProjetoCommand { ProjetoId = origem, PecaDoProjetoId = pecaId });
        var importada = await AmbienteDeTeste.SendAsync(new ImportarPecaDaBibliotecaCommand { ProjetoId = destino, PecaId = naBiblioteca });

        try
        {
            var desenhos = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.AsNoTracking().Include(p => p.Desenhos).SingleAsync(p => p.Id == naBiblioteca));
            desenhos.Desenhos.Select(d => (d.Ferramenta, Encoding.UTF8.GetString(d.Conteudo))).Should().BeEquivalentTo([(Desenho, "DXF final"), (Modelo, "3D final")], "vai o arquivo final, não o mais recente");

            (await ConteudoAsync(destino, Desenho, pecaId: importada)).Should().Be("DXF final");
            (await ConteudoAsync(destino, Modelo, pecaId: importada)).Should().Be("3D final");
            var finalImportado = await AmbienteDeTeste.SendAsync(new GetArquivoFerramentaQuery { ProjetoId = destino, PecaDoProjetoId = importada, Ferramenta = Modelo, Final = true });
            Encoding.UTF8.GetString(finalImportado!.Conteudo).Should().Be("3D final", "a cópia trazida já é o arquivo final");
            var historico = await AmbienteDeTeste.SendAsync(new GetHistoricoArquivoFerramentaQuery { ProjetoId = destino, PecaDoProjetoId = importada, Ferramenta = Modelo });
            historico.Should().ContainSingle().Which.Origem.Should().Be(OrigemArquivo.Biblioteca);
            (await LerAsync(destino)).Pecas.Single().Should().BeEquivalentTo(
                new { Status = StatusPecaDoProjeto.Publicada, PecaId = (long?)naBiblioteca },
                "a cópia trazida da biblioteca não é uma alteração do desenho");

            var lida = await cliente.ExecutarAsync("query ($id: Long!) { peca(id: $id) { desenhos { ferramenta tamanho copiadoEm } } }", new { id = naBiblioteca });
            lida["errors"].Should().BeNull();
            lida["data"]!["peca"]!["desenhos"]!.AsArray().Select(d => (d!["ferramenta"]!.GetValue<string>(), d["tamanho"]!.GetValue<int>()))
                .Should().Equal((Desenho, 9), (Modelo, 8));

            using var arquivo = await SendAsync(http, cliente, HttpMethod.Get, $"/api/pecas/{naBiblioteca}/desenhos/{Modelo}");
            using var semDesenho = await SendAsync(http, cliente, HttpMethod.Get, $"/api/pecas/{naBiblioteca}/desenhos/briefing");
            arquivo.StatusCode.Should().Be(HttpStatusCode.OK);
            (await arquivo.Content.ReadAsStringAsync()).Should().Be("3D final");
            semDesenho.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
        finally
        {
            await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Database.ExecuteSqlAsync($"UPDATE projeto_pecas SET peca_id = NULL WHERE peca_id = {naBiblioteca}"));
            await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.Where(p => p.Id == naBiblioteca).ExecuteDeleteAsync());
        }
    }

    [Test]
    public async Task DeveDarOPesoDaPecaPeloModelo3dNaLigaDelaOuODigitadoAsync()
    {
        using var http = AmbienteDeTeste.CriarClienteHttp();
        AmbienteDeTeste.Relogio.Agora = DateTimeOffset.UtcNow;
        var cliente = await EntrarAsync(http, Usar(new UsuarioBuilder()).Admin());
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var elo = await Usar(new PecaDoProjetoBuilder(projetoId)).CustomizeCommand(c => (c.Tipo, c.LigaId, c.Material, c.PesoGramas) = (TipoPeca.Elo, 2, null, 1m)).BuildAsync();
        var pedra = await Usar(new PecaDoProjetoBuilder(projetoId)).CustomizeCommand(c => (c.Tipo, c.LigaId, c.Material, c.PesoGramas) = (TipoPeca.Pedra, null, "Turmalina", 0.2m)).BuildAsync();

        await SalvarAsync(projetoId, Modelo, "3D do elo sem final", pecaId: elo);
        var semModelo = await PesoAsync(elo);
        await SalvarFinalAsync(projetoId, Modelo, "3D do elo", elo);
        await SalvarFinalAsync(projetoId, Modelo, "3D da pedra", pedra);
        AmbienteDeTeste.Medidor.Medicao = new MedicaoDoModelo(
            [
                new CorpoMedido("Elo", "Castwork liga 3", 100m, 1.5m, Fechado: true),
                new CorpoMedido("Solda", MateriaisDoModelo.SemMaterial, 20m, 0.03m, Fechado: true),
                new CorpoMedido("Brilhante", MateriaisDoModelo.Pedra, 30m, 0.1m, Fechado: true),
            ],
            []);
        var medido = await PesoAsync(elo);
        var daPedra = await PesoAsync(pedra);
        var lido = await cliente.ExecutarAsync("query ($id: Long!) { projeto(id: $id) { pecas { id peso { gramas origem pesoDigitado } } } }", new { id = projetoId });
        var soUma = await cliente.ExecutarAsync("query ($id: Long!, $peca: Long!) { projeto(id: $id) { pecas(id: $peca) { id } } }", new { id = projetoId, peca = pedra });
        AmbienteDeTeste.Medidor.Medicao = null;
        var naoMedido = await PesoAsync(elo);

        semModelo.Should().Be(new PesoDaPecaDto(elo, 1m, OrigemDoPesoDaPeca.Digitado, 1m), "sem 3D final, vale o digitado");
        medido.Should().Be(new PesoDaPecaDto(elo, 1.86m, OrigemDoPesoDaPeca.Modelo, 1m), "120 mm³ × 15,5 g/cm³ (Ouro 18k amarelo, a liga da peça)");
        daPedra.Should().Be(new PesoDaPecaDto(pedra, 0.2m, OrigemDoPesoDaPeca.ModeloSemLiga, 0.2m));
        naoMedido.Should().Be(new PesoDaPecaDto(elo, null, OrigemDoPesoDaPeca.ModeloNaoMedido, 1m));
        lido["errors"].Should().BeNull();
        var pesos = lido["data"]!["projeto"]!["pecas"]!.AsArray().ToDictionary(p => p!["id"]!.GetValue<long>(), p => p!["peso"]!);
        (pesos[elo]["gramas"]!.GetValue<decimal>(), pesos[elo]["origem"]!.GetValue<string>()).Should().Be((1.86m, "MODELO"));
        pesos[pedra]["origem"]!.GetValue<string>().Should().Be("MODELO_SEM_LIGA");
        soUma["data"]!["projeto"]!["pecas"]!.AsArray().Select(p => p!["id"]!.GetValue<long>()).Should().Equal([pedra], "a página da peça pede (e mede) só ela");
    }

    [Test]
    public async Task DeveMontarOModeloDaVersaoAPartirDasPecasAsync()
    {
        using var http = AmbienteDeTeste.CriarClienteHttp();
        AmbienteDeTeste.Relogio.Agora = DateTimeOffset.UtcNow;
        var cliente = await EntrarAsync(http, Usar(new UsuarioBuilder()).Admin());
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var versaoId = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Base" });
        var elo = await Usar(new PecaDoProjetoBuilder(projetoId)).CustomizeCommand(c => (c.Nome, c.Tipo, c.LigaId, c.Material) = ("Elo oval", TipoPeca.Elo, 2, null)).BuildAsync();
        var fecho = await Usar(new PecaDoProjetoBuilder(projetoId)).CustomizeCommand(c => (c.Nome, c.Tipo, c.LigaId, c.Material) = ("Fecho", TipoPeca.Fecho, 2, null)).BuildAsync();
        await AmbienteDeTeste.SendAsync(new AdicionarItemCommand { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = elo, Quantidade = 3 });
        await AmbienteDeTeste.SendAsync(new AdicionarItemCommand { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = fecho, Quantidade = 1 });
        await SalvarFinalAsync(projetoId, Modelo, "3D do elo", elo);
        await SalvarAsync(projetoId, Modelo, "3D do elo depois do final", pecaId: elo);
        await SalvarAsync(projetoId, Modelo, "3D do fecho sem final", pecaId: fecho);
        var anterior = await SalvarAsync(projetoId, Modelo, "{}", versaoId: versaoId);

        var montado = await AmbienteDeTeste.SendAsync(new MontarAPartirDasPecasCommand { ProjetoId = projetoId, VersaoId = versaoId });
        using var pelaRota = await SendAsync(http, cliente, HttpMethod.Post, $"/api/projetos/{projetoId}/versoes/{versaoId}/modelo-3d/montar-a-partir-das-pecas");

        montado.ArquivoAnteriorId.Should().Be(anterior);
        montado.Pecas.Select(p => (p.Nome, p.Quantidade)).Should().Equal(("Elo oval", 3));
        montado.PecasSemModelo.Should().Equal("Fecho");
        var (arquivoDaVersao, pecas) = AmbienteDeTeste.Montador.Chamadas[0];
        Encoding.UTF8.GetString(arquivoDaVersao!).Should().Be("{}");
        pecas.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Nome = "Elo oval", Quantidade = 3, MaterialDaLiga = "Castwork liga 2" });
        Encoding.UTF8.GetString(pecas[0].Arquivo).Should().Be("3D do elo");
        var historico = await AmbienteDeTeste.SendAsync(new GetHistoricoArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Modelo });
        historico[1].Should().BeEquivalentTo(new { Id = montado.ArquivoId, Origem = OrigemArquivo.Montagem });
        pelaRota.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ConteudoAsync(projetoId, Modelo, versaoId: versaoId)).Should().Be("{}\nElo oval × 3\nElo oval × 3", "a segunda montagem traz as peças de novo sobre o modelo montado");
    }

    [Test]
    public async Task DeveRecusarMontarSemPecaComModeloEmVersaoPublicadaOuComAIaTrabalhandoAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var versaoId = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Base" });
        var publicada = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Fechada" });
        await AmbienteDeTeste.SendAsync(new PublicarVersaoCommand { ProjetoId = projetoId, VersaoId = publicada });
        var elo = await Usar(new PecaDoProjetoBuilder(projetoId)).BuildAsync();
        await AmbienteDeTeste.SendAsync(new AdicionarItemCommand { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = elo, Quantidade = 2 });

        var semModelo = () => AmbienteDeTeste.SendAsync(new MontarAPartirDasPecasCommand { ProjetoId = projetoId, VersaoId = versaoId });
        var naPublicada = () => AmbienteDeTeste.SendAsync(new MontarAPartirDasPecasCommand { ProjetoId = projetoId, VersaoId = publicada });
        (await semModelo.Should().ThrowAsync<RegraDeNegocioException>()).WithMessage("Nenhuma peça da composição tem modelo 3D*");
        await naPublicada.Should().ThrowAsync<RegraDeNegocioException>();

        await SalvarAsync(projetoId, Modelo, "3D do elo", pecaId: elo);
        await AmbienteDeTeste.ExecutarNoEscopoAsync(async sp =>
        {
            var db = sp.GetRequiredService<Castwork.Infra.Data.ApplicationDbContext>();
            var conversa = ConversaFerramenta.Criar(projetoId, versaoId, Modelo, AmbienteDeTeste.Relogio.GetUtcNow());
            conversa.RegistrarFala("Modele o colar", [], AmbienteDeTeste.Relogio.GetUtcNow());
            db.ConversasFerramenta.Add(conversa);
            return await db.SaveChangesAsync();
        });
        var comIa = () => AmbienteDeTeste.SendAsync(new MontarAPartirDasPecasCommand { ProjetoId = projetoId, VersaoId = versaoId });

        (await comIa.Should().ThrowAsync<RegraDeNegocioException>()).WithMessage("A IA está editando o modelo*");
        AmbienteDeTeste.Montador.Chamadas.Should().BeEmpty();
    }

    [Test]
    public async Task DeveManterAsConversasAntigasNaMigrationDesenhoDasPecasAsync()
    {
        long projetoId, versaoId, arquivoId;

        // Volta o banco para antes desta etapa, grava conversas do projeto e da versão no formato antigo e aplica de novo.
        await MigrarAsync(MigrationAnterior);
        try
        {
            projetoId = await InserirAsync($"INSERT INTO projetos (nome, criado_em) VALUES ('Anel antigo', now()) RETURNING id");
            versaoId = await InserirAsync($"INSERT INTO projeto_versoes (projeto_id, numero, nome, status) VALUES ({projetoId}, 1, 'Base', 'EmDesenvolvimento') RETURNING id");
            await InserirAsync($"INSERT INTO ferramenta_conversas (projeto_id, versao_id, ferramenta, criada_em, chat_atual) VALUES ({projetoId}, NULL, 'briefing', now(), 1) RETURNING id");
            var daVersao = await InserirAsync($"INSERT INTO ferramenta_conversas (projeto_id, versao_id, ferramenta, criada_em, chat_atual) VALUES ({projetoId}, {versaoId}, 'desenho-2d', now(), 1) RETURNING id");
            arquivoId = await InserirAsync($"INSERT INTO ferramenta_arquivos (conversa_id, conteudo, origem, criado_em) VALUES ({daVersao}, {"DXF antigo"u8.ToArray()}, 2, now()) RETURNING id");
        }
        finally
        {
            await MigrarAsync(null);
        }

        try
        {
            var conversas = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.ConversasFerramenta.AsNoTracking().Where(c => c.ProjetoId == projetoId).ToListAsync());
            conversas.Select(c => (c.Ferramenta, c.VersaoId, c.PecaDoProjetoId)).Should().BeEquivalentTo([("briefing", (long?)null, (long?)null), (Desenho, (long?)versaoId, (long?)null)]);
            (await ConteudoAsync(projetoId, Desenho, versaoId: versaoId)).Should().Be("DXF antigo");

            // O desenho da peça convive com a conversa do projeto (as duas sem versão) e com a da versão.
            var peca = await Usar(new PecaDoProjetoBuilder(projetoId)).BuildAsync();
            await SalvarAsync(projetoId, Desenho, "DXF da peça", pecaId: peca);
            (await ConteudoAsync(projetoId, Desenho, pecaId: peca)).Should().Be("DXF da peça");
            (await ConteudoAsync(projetoId, Desenho, versaoId: versaoId)).Should().Be("DXF antigo");
            arquivoId.Should().BePositive();
        }
        finally
        {
            await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Projetos.Where(p => p.Id == projetoId).ExecuteDeleteAsync());
        }
    }

    [Test]
    public async Task DeveMarcarOArquivoAtualComoFinalNaMigrationArquivoFinalDasPecasAsync()
    {
        long projetoId, modelo, desenho, daVersao, atual;

        // Volta o banco para antes do arquivo final e grava conversas da peça e da versão como eram.
        await MigrarAsync("20261110200014_ImagensDosArquivos");
        try
        {
            projetoId = await InserirAsync($"INSERT INTO projetos (nome, criado_em) VALUES ('Colar antigo', now()) RETURNING id");
            var versaoId = await InserirAsync($"INSERT INTO projeto_versoes (projeto_id, numero, nome, status) VALUES ({projetoId}, 1, 'Base', 'EmDesenvolvimento') RETURNING id");
            var pecaId = await InserirAsync($"INSERT INTO projeto_pecas (projeto_id, nome, tipo, ativo, status) VALUES ({projetoId}, 'Elo', 'Elo', true, 'Rascunho') RETURNING id");
            modelo = await InserirAsync($"INSERT INTO ferramenta_conversas (projeto_id, peca_do_projeto_id, ferramenta, criada_em, chat_atual) VALUES ({projetoId}, {pecaId}, 'modelo-3d', now(), 1) RETURNING id");
            desenho = await InserirAsync($"INSERT INTO ferramenta_conversas (projeto_id, peca_do_projeto_id, ferramenta, criada_em, chat_atual) VALUES ({projetoId}, {pecaId}, 'desenho-2d', now(), 1) RETURNING id");
            daVersao = await InserirAsync($"INSERT INTO ferramenta_conversas (projeto_id, versao_id, ferramenta, criada_em, chat_atual) VALUES ({projetoId}, {versaoId}, 'modelo-3d', now(), 1) RETURNING id");
            atual = await InserirAsync($"INSERT INTO ferramenta_arquivos (conversa_id, conteudo, origem, criado_em) VALUES ({modelo}, {"3D atual"u8.ToArray()}, 2, now()) RETURNING id");
            await InserirAsync($"INSERT INTO ferramenta_arquivos (conversa_id, conteudo, origem, criado_em, arquivado_em) VALUES ({modelo}, {"3D arquivado"u8.ToArray()}, 2, now(), now()) RETURNING id");
            await InserirAsync($"INSERT INTO ferramenta_arquivos (conversa_id, conteudo, origem, criado_em) VALUES ({desenho}, {"DXF"u8.ToArray()}, 2, now()) RETURNING id");
            await InserirAsync($"INSERT INTO ferramenta_arquivos (conversa_id, conteudo, origem, criado_em) VALUES ({desenho}, {Array.Empty<byte>()}, 3, now()) RETURNING id");
            await InserirAsync($"INSERT INTO ferramenta_arquivos (conversa_id, conteudo, origem, criado_em) VALUES ({daVersao}, {"3D da versão"u8.ToArray()}, 2, now()) RETURNING id");
        }
        finally
        {
            await MigrarAsync(null);
        }

        try
        {
            var finais = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.ConversasFerramenta.AsNoTracking().Where(c => c.ProjetoId == projetoId).ToDictionaryAsync(c => c.Id, c => c.ArquivoFinalId));
            finais[modelo].Should().Be(atual, "o atual (o mais recente não arquivado) vira o final");
            finais[desenho].Should().BeNull("o atual era o desenho em branco: a peça fica sem final");
            finais[daVersao].Should().BeNull("a versão não tem arquivo final");
        }
        finally
        {
            await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Projetos.Where(p => p.Id == projetoId).ExecuteDeleteAsync());
        }
    }

    private static Task<long> SalvarAsync(long projetoId, string ferramenta, string conteudo, long versaoId = 0, long pecaId = 0)
        => AmbienteDeTeste.SendAsync(new SalvarArquivoFerramentaCommand
        {
            ProjetoId = projetoId,
            VersaoId = versaoId,
            PecaDoProjetoId = pecaId,
            Ferramenta = ferramenta,
            Conteudo = Convert.ToBase64String(Encoding.UTF8.GetBytes(conteudo)),
        });

    private static async Task<long> SalvarFinalAsync(long projetoId, string ferramenta, string conteudo, long pecaId)
    {
        var arquivoId = await SalvarAsync(projetoId, ferramenta, conteudo, pecaId: pecaId);
        await FinalAsync(projetoId, ferramenta, arquivoId, pecaId);
        return arquivoId;
    }

    private static Task<ArquivoFinalDto> FinalAsync(long projetoId, string ferramenta, long arquivoId, long pecaId)
        => AmbienteDeTeste.SendAsync(new MarcarArquivoFinalFerramentaCommand { ProjetoId = projetoId, PecaDoProjetoId = pecaId, Ferramenta = ferramenta, ArquivoId = arquivoId });

    private static async Task<string?> ConteudoAsync(long projetoId, string ferramenta, long versaoId = 0, long pecaId = 0)
    {
        var arquivo = await AmbienteDeTeste.SendAsync(new GetArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaId, Ferramenta = ferramenta });
        return arquivo is null ? null : Encoding.UTF8.GetString(arquivo.Conteudo);
    }

    private static Task<PesoDaPecaDto> PesoAsync(long pecaId) => AmbienteDeTeste.SendAsync(new GetPesoDaPecaQuery { PecaDoProjetoId = pecaId });

    private static Task MigrarAsync(string? alvo)
        => AmbienteDeTeste.ExecutarNoBancoAsync(db => db.GetService<IMigrator>().MigrateAsync(alvo));

    /// <summary>
    /// INSERT ... RETURNING não aceita composição: lê a lista e pega o único Id no cliente.
    /// </summary>
    private static async Task<long> InserirAsync(FormattableString sql)
        => (await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Database.SqlQuery<long>(sql).ToListAsync())).Single();

    private static Task<Projeto> LerAsync(long id)
        => AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Projetos.Include(p => p.Pecas).AsNoTracking().SingleAsync(p => p.Id == id));

    private static async Task<ClienteGraphQl> EntrarAsync(HttpClient http, UsuarioBuilder usuarios)
    {
        var command = await usuarios.BuildCommandAsync();
        await usuarios.SendAsync(command);
        var sessao = await AmbienteDeTeste.SendAsync(new LoginCommand { Email = command.Email, Senha = command.Senha });
        return new ClienteGraphQl(http, sessao.AccessToken);
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient http, ClienteGraphQl cliente, HttpMethod metodo, string url)
    {
        using var requisicao = new HttpRequestMessage(metodo, new Uri(url, UriKind.Relative));
        requisicao.Headers.Authorization = new("Bearer", cliente.Token);
        return await http.SendAsync(requisicao);
    }
}
