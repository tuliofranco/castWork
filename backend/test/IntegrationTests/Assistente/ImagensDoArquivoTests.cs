using System.Net;
using System.Text;

using Castwork.Application.Acesso.Commands.Login;
using Castwork.Application.Assistente.Commands;
using Castwork.Application.Assistente.Queries;
using Castwork.Application.Pecas.Queries;
using Castwork.Application.Projetos.Commands.ImportarPecaDaBiblioteca;
using Castwork.Application.Projetos.Commands.PublicarPecaDoProjeto;
using Castwork.Application.Projetos.Queries.GetFichaTecnicaDaPeca;
using Castwork.IntegrationTests.GraphQl;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Assistente;

/// <summary>
/// As imagens guardadas de cada arquivo novo do desenho 2D e do modelo 3D, com o gerador de teste: geradas em segundo plano
/// depois de salvar (sem atrasar a resposta), uma falha que não para a fila, a leitura (lista e binário, da peça do projeto
/// e da biblioteca), a cópia ao publicar e ao importar e a ficha técnica com a iso do 3D e o 2D.
/// </summary>
public class ImagensDoArquivoTests : TesteDeIntegracao
{
    private const string Desenho = "desenho-2d";
    private const string Modelo = "modelo-3d";

    [Test]
    public async Task DeveGerarAsQuatroVistasDoModelo3dDepoisDeSalvarAsync()
    {
        var (projetoId, pecaId) = await PecaAsync();

        var arquivoId = await SalvarAsync(projetoId, Modelo, "3D do elo", pecaId);
        await AguardarImagensAsync(arquivoId, 4);

        var lista = await AmbienteDeTeste.SendAsync(new GetImagensDoArquivoQuery { ProjetoId = projetoId, PecaDoProjetoId = pecaId, Ferramenta = Modelo });
        lista.ArquivoId.Should().Be(arquivoId);
        lista.Imagens.Select(i => (i.Vista, i.Tipo)).Should().Equal(("iso", "image/png"), ("frente", "image/png"), ("topo", "image/png"), ("lado", "image/png"));
        var topo = await AmbienteDeTeste.SendAsync(new GetImagemDoArquivoQuery { ProjetoId = projetoId, PecaDoProjetoId = pecaId, Ferramenta = Modelo, Vista = "topo" });
        topo!.Conteudo.Should().Equal(GeradorDeImagensDeTeste.Png("topo", Encoding.UTF8.GetBytes("3D do elo")));
        (await AmbienteDeTeste.SendAsync(new GetImagemDoArquivoQuery { ProjetoId = projetoId, PecaDoProjetoId = pecaId, Ferramenta = Modelo, Vista = "2d" })).Should().BeNull();
    }

    [Test]
    public async Task DeveGuardarOSvgEOPngDoDesenho2dEServirOPedidoAsync()
    {
        var (projetoId, pecaId) = await PecaAsync();

        await SalvarAsync(projetoId, Desenho, "DXF antigo", pecaId);
        var atual = await SalvarAsync(projetoId, Desenho, "DXF atual", pecaId);
        await AguardarImagensAsync(atual, 2);

        var melhor = await ImagemAsync(projetoId, pecaId, Desenho, "2d", formato: null);
        var png = await ImagemAsync(projetoId, pecaId, Desenho, "2d", "png");
        melhor!.Tipo.Should().Be("image/svg+xml", "sem formato, a de melhor qualidade");
        Encoding.UTF8.GetString(melhor.Conteudo).Should().Contain("DXF atual", "a do arquivo atual");
        png!.Tipo.Should().Be("image/png");
        (await ImagemAsync(projetoId, pecaId, Desenho, "2d", "gif")).Should().BeNull();
    }

    [Test]
    public async Task DeveSeguirAFilaQuandoUmaGeracaoFalhaAsync()
    {
        var (projetoId, pecaId) = await PecaAsync();
        AmbienteDeTeste.GeradorDeImagens.Falhar();

        var comFalha = await SalvarAsync(projetoId, Modelo, "3D que falha", pecaId);
        var seguinte = await SalvarAsync(projetoId, Modelo, "3D seguinte", pecaId);
        await AguardarImagensAsync(seguinte, 4);

        (await QuantidadeAsync(comFalha)).Should().Be(0, "a falha vai para o log e o arquivo fica com a prévia sob demanda");
        var lista = await AmbienteDeTeste.SendAsync(new GetImagensDoArquivoQuery { ProjetoId = projetoId, PecaDoProjetoId = pecaId, Ferramenta = Modelo });
        lista.Imagens.Should().HaveCount(4);
    }

    [Test]
    public async Task DeveCopiarAsImagensAoPublicarEAoImportarAsync()
    {
        using var http = AmbienteDeTeste.CriarClienteHttp();
        AmbienteDeTeste.Relogio.Agora = DateTimeOffset.UtcNow;
        var cliente = await EntrarAsync(http, Usar(new UsuarioBuilder()).Admin());
        var (origem, pecaId) = await PecaAsync();
        var destino = await Usar(new ProjetoBuilder()).BuildAsync();
        await AguardarImagensAsync(await SalvarAsync(origem, Desenho, "DXF do elo", pecaId), 2);
        await AguardarImagensAsync(await SalvarAsync(origem, Modelo, "3D do elo", pecaId), 4);

        var naBiblioteca = await AmbienteDeTeste.SendAsync(new PublicarPecaDoProjetoCommand { ProjetoId = origem, PecaDoProjetoId = pecaId });
        var importada = await AmbienteDeTeste.SendAsync(new ImportarPecaDaBibliotecaCommand { ProjetoId = destino, PecaId = naBiblioteca });

        try
        {
            var daBiblioteca = await AmbienteDeTeste.SendAsync(new GetImagensDoDesenhoDaPecaQuery { PecaId = naBiblioteca, Ferramenta = Modelo });
            daBiblioteca.Select(i => i.Vista).Should().Equal("iso", "frente", "topo", "lado");
            (await AmbienteDeTeste.SendAsync(new GetImagensDoDesenhoDaPecaQuery { PecaId = naBiblioteca, Ferramenta = Desenho })).Select(i => i.Tipo)
                .Should().Equal("image/svg+xml", "image/png");

            var importadas = await AmbienteDeTeste.SendAsync(new GetImagensDoArquivoQuery { ProjetoId = destino, PecaDoProjetoId = importada, Ferramenta = Modelo });
            importadas.Imagens.Should().HaveCount(4, "copiadas da biblioteca, sem gerar de novo nem duplicar");
            var iso = await ImagemAsync(destino, importada, Modelo, "iso", "png");
            iso!.Conteudo.Should().Equal(GeradorDeImagensDeTeste.Png("iso", Encoding.UTF8.GetBytes("3D do elo")));

            using var daPeca = await GetAsync(http, cliente, $"/api/projetos/{destino}/pecas/{importada}/ferramentas/{Modelo}/arquivo/imagens/frente");
            using var lista = await GetAsync(http, cliente, $"/api/projetos/{destino}/pecas/{importada}/ferramentas/{Modelo}/arquivo/imagens");
            using var daPecaDaBiblioteca = await GetAsync(http, cliente, $"/api/pecas/{naBiblioteca}/desenhos/{Desenho}/imagens/2d?formato=png");
            using var semVista = await GetAsync(http, cliente, $"/api/pecas/{naBiblioteca}/desenhos/{Desenho}/imagens/iso");
            daPeca.StatusCode.Should().Be(HttpStatusCode.OK);
            daPeca.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
            (await lista.Content.ReadAsStringAsync()).Should().Contain("\"vista\":\"frente\"");
            daPecaDaBiblioteca.StatusCode.Should().Be(HttpStatusCode.OK);
            daPecaDaBiblioteca.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
            semVista.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
        finally
        {
            await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Database.ExecuteSqlAsync($"UPDATE projeto_pecas SET peca_id = NULL WHERE peca_id = {naBiblioteca}"));
            await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.Where(p => p.Id == naBiblioteca).ExecuteDeleteAsync());
        }
    }

    [Test]
    public async Task DeveLevarAIsoDo3dEO2dParaAFichaTecnicaAsync()
    {
        var (projetoId, pecaId) = await PecaAsync();
        var semImagens = Encoding.UTF8.GetString(await AmbienteDeTeste.SendAsync(new GetFichaTecnicaDaPecaQuery { ProjetoId = projetoId, PecaDoProjetoId = pecaId }));
        await AguardarImagensAsync(await SalvarAsync(projetoId, Desenho, "DXF do elo", pecaId), 2);
        await AguardarImagensAsync(await SalvarAsync(projetoId, Modelo, "3D do elo", pecaId), 4);

        var ficha = Encoding.UTF8.GetString(await AmbienteDeTeste.SendAsync(new GetFichaTecnicaDaPecaQuery { ProjetoId = projetoId, PecaDoProjetoId = pecaId }));

        semImagens.Should().NotContain("Imagem:");
        ficha.Split('\n').Where(l => l.StartsWith("Imagem:", StringComparison.Ordinal)).Should().Equal(
            $"Imagem: Modelo 3D ({GeradorDeImagensDeTeste.Png("iso", Encoding.UTF8.GetBytes("3D do elo")).Length} bytes)",
            $"Imagem: Desenho 2D ({GeradorDeImagensDeTeste.Png("2d", Encoding.UTF8.GetBytes("DXF do elo")).Length} bytes)");
    }

    /// <summary>
    /// A geração é em segundo plano: espera as imagens aparecerem no banco (o gerador de teste é imediato).
    /// </summary>
    private static async Task AguardarImagensAsync(long arquivoId, int quantidade)
    {
        var limite = DateTime.UtcNow.AddSeconds(10);
        while (await QuantidadeAsync(arquivoId) < quantidade)
        {
            DateTime.UtcNow.Should().BeBefore(limite, $"o arquivo {arquivoId} devia ter {quantidade} imagens");
            await Task.Delay(50);
        }
    }

    private static Task<int> QuantidadeAsync(long arquivoId)
        => AmbienteDeTeste.ExecutarNoBancoAsync(db => db.ImagensDoArquivo.CountAsync(i => i.ArquivoId == arquivoId));

    /// <summary>
    /// Salva e marca como o arquivo final da peça (o que vai para a biblioteca e para a ficha técnica).
    /// </summary>
    private static async Task<long> SalvarAsync(long projetoId, string ferramenta, string conteudo, long pecaId)
    {
        var arquivoId = await AmbienteDeTeste.SendAsync(new SalvarArquivoFerramentaCommand
        {
            ProjetoId = projetoId,
            PecaDoProjetoId = pecaId,
            Ferramenta = ferramenta,
            Conteudo = Convert.ToBase64String(Encoding.UTF8.GetBytes(conteudo)),
        });
        await AmbienteDeTeste.SendAsync(new MarcarArquivoFinalFerramentaCommand { ProjetoId = projetoId, PecaDoProjetoId = pecaId, Ferramenta = ferramenta, ArquivoId = arquivoId });
        return arquivoId;
    }

    private static Task<ConteudoDaImagemDto?> ImagemAsync(long projetoId, long pecaId, string ferramenta, string vista, string? formato)
        => AmbienteDeTeste.SendAsync(new GetImagemDoArquivoQuery { ProjetoId = projetoId, PecaDoProjetoId = pecaId, Ferramenta = ferramenta, Vista = vista, Formato = formato });

    private static async Task<ClienteGraphQl> EntrarAsync(HttpClient http, UsuarioBuilder usuarios)
    {
        var command = await usuarios.BuildCommandAsync();
        await usuarios.SendAsync(command);
        var sessao = await AmbienteDeTeste.SendAsync(new LoginCommand { Email = command.Email, Senha = command.Senha });
        return new ClienteGraphQl(http, sessao.AccessToken);
    }

    private static async Task<HttpResponseMessage> GetAsync(HttpClient http, ClienteGraphQl cliente, string url)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, new Uri(url, UriKind.Relative));
        requisicao.Headers.Authorization = new("Bearer", cliente.Token);
        return await http.SendAsync(requisicao);
    }

    private async Task<(long ProjetoId, long PecaId)> PecaAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        return (projetoId, await Usar(new PecaDoProjetoBuilder(projetoId)).BuildAsync());
    }
}
