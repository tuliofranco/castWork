using System.Text.Json;

using Castwork.Application.Common.Exceptions;
using Castwork.Application.Ferramentas.Modelo3d;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;
using Castwork.Infra.IA.Assistente.Mcp;
using Castwork.Infra.IA.Ferramentas.Modelo3d;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Castwork.IntegrationTests.Ferramentas.Modelo3d;

/// <summary>
/// O editor da conferência de peso e materiais com o SolidCraft de teste, sem IA: um anel feito sem os materiais do ateliê
/// (aro e garra de ouro sobrepostos e um pino sem material) recebe o material do pino e tem aro e garra unidos; a medição
/// do arquivo alterado confirma. Precisa do <c>solidcraft-cli</c> (<c>SolidCraft:Caminho</c> nos User Secrets da API ou
/// <c>SolidCraft__Caminho</c>); sem ele, fica ignorado.
/// </summary>
public class EditorDoModeloTests
{
    /// <summary>
    /// Sem os materiais do ateliê: o editor precisa garanti-los antes do <c>material.assign</c>.
    /// </summary>
    private const string Corpos = """
        {"commands":[
          {"command":"sketch.create","params":{"plane":"XY"}},
          {"command":"sketch.circle.center","params":{"center":[0,0],"radius":5}},
          {"command":"sketch.finish","params":{}},
          {"command":"solid.extrude","params":{"distance":"2","body_name":"Aro"}},
          {"command":"sketch.create","params":{"plane":"XY"}},
          {"command":"sketch.circle.center","params":{"center":[4,0],"radius":2}},
          {"command":"sketch.finish","params":{}},
          {"command":"solid.extrude","params":{"distance":"2","body_name":"Garra","operation":"new"}},
          {"command":"sketch.create","params":{"plane":"XY"}},
          {"command":"sketch.circle.center","params":{"center":[60,0],"radius":1}},
          {"command":"sketch.finish","params":{}},
          {"command":"solid.extrude","params":{"distance":"2","body_name":"Pino","operation":"new"}}
        ]}
        """;

    private static readonly TimeSpan TempoLimite = TimeSpan.FromSeconds(60);

    [Test]
    public async Task DeveDefinirOMaterialEUnirOsCorposSemIaAsync()
    {
        var opcoes = Options.Create(new SolidCraftOptions { Caminho = CaminhoDoSolidCraft() });
        var editor = new EditorDoModeloSolidCraft(opcoes, NullLogger<EditorDoModeloSolidCraft>.Instance);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var medidor = new MedidorDoModeloSolidCraft(opcoes, cache, NullLogger<MedidorDoModeloSolidCraft>.Instance);
        var anel = await CriarAnelAsync(opcoes.Value);

        var definicao = new DefinicaoDeMateriais(
        [
            new MaterialDoCorpo("Aro", "Castwork liga 2"),
            new MaterialDoCorpo("Garra", "Castwork liga 2"),
            new MaterialDoCorpo("Pino", MateriaisDoModelo.Apoio),
        ]);
        var comMaterial = await editor.AlterarAsync(anel, Materiais(), definicao, CancellationToken.None);
        var medicao = await medidor.MedirAsync(new ArquivoParaMedir(1, _ => Task.FromResult(comMaterial)), Materiais(), CancellationToken.None);

        medicao!.Corpos.Select(c => (c.Nome, c.Material)).Should().Equal(("Aro", "Castwork liga 2"), ("Garra", "Castwork liga 2"), ("Pino", MateriaisDoModelo.Apoio));
        medicao.Sobreposicoes.Should().ContainSingle();
        PesoDoModelo.Classificar(medicao).PesoPorLiga().Single().Peso.Should().Be(Peso.EmGramas(2.824m), "a parte comum conta duas vezes");

        var unido = await editor.AlterarAsync(comMaterial, Materiais(), new UniaoDeCorpos("Aro", ["Garra"]), CancellationToken.None);
        var depois = await medidor.MedirAsync(new ArquivoParaMedir(2, _ => Task.FromResult(unido)), Materiais(), CancellationToken.None);

        depois!.Corpos.Select(c => (c.Nome, c.Material)).Should().Equal(("Aro", "Castwork liga 2"), ("Pino", MateriaisDoModelo.Apoio));
        depois.Sobreposicoes.Should().BeEmpty();
        PesoDoModelo.Classificar(depois).PesoPorLiga().Single().Peso.Should().Be(Peso.EmGramas(2.524m), "a parte comum agora conta uma vez só");
    }

    [Test]
    public async Task DeveRecusarCorpoQueNaoExisteSemAlterarOArquivoAsync()
    {
        var opcoes = Options.Create(new SolidCraftOptions { Caminho = CaminhoDoSolidCraft() });
        var editor = new EditorDoModeloSolidCraft(opcoes, NullLogger<EditorDoModeloSolidCraft>.Instance);
        var anel = await CriarAnelAsync(opcoes.Value);

        await FluentActions.Awaiting(() => editor.AlterarAsync(anel, Materiais(), new DefinicaoDeMateriais([new MaterialDoCorpo("Garra", "Pedra"), new MaterialDoCorpo("Nada", "Pedra")]), CancellationToken.None))
            .Should().ThrowAsync<RegraDeNegocioException>().WithMessage("O SolidCraft não conseguiu definir o material dos corpos. Confira o modelo e tente de novo.");
    }

    [Test]
    public async Task DeveDizerQueOSolidCraftNaoEstaConfiguradoAsync()
    {
        var editor = new EditorDoModeloSolidCraft(Options.Create(new SolidCraftOptions()), NullLogger<EditorDoModeloSolidCraft>.Instance);

        await FluentActions.Awaiting(() => editor.AlterarAsync([1], Materiais(), new UniaoDeCorpos("Aro", ["Garra"]), CancellationToken.None))
            .Should().ThrowAsync<ServicoIndisponivelException>();
    }

    private static IReadOnlyList<MaterialDoModelo> Materiais()
        => MateriaisDoModelo.Montar([(2, "Ouro 18k amarelo", Metal.Ouro, 15.5m), (7, "Prata 925", Metal.Prata, 10.36m)]);

    private static async Task<byte[]> CriarAnelAsync(SolidCraftOptions opcoes)
    {
        var definicao = Modelo3dNaIa.Criar(opcoes);
        var pasta = Directory.CreateTempSubdirectory("castwork-teste-editor-");
        try
        {
            var caminho = Path.Combine(pasta.FullName, "anel.json");
            await using (var mcp = await ClienteMcp.IniciarAsync(definicao.Executavel!, definicao.Argumentos, pasta.FullName, TempoLimite, CancellationToken.None))
            {
                await ChamarAsync(mcp, definicao.NovoArquivo);
                await ChamarAsync(mcp, ("batch", Corpos));
                await ChamarAsync(mcp, definicao.SalvarArquivo(caminho));
            }

            return await File.ReadAllBytesAsync(caminho);
        }
        finally
        {
            pasta.Delete(recursive: true);
        }
    }

    private static async Task ChamarAsync(ClienteMcp mcp, (string Nome, string Argumentos) chamada)
    {
        var resultado = await mcp.ChamarFerramentaAsync(chamada.Nome, JsonDocument.Parse(chamada.Argumentos).RootElement, CancellationToken.None);
        resultado.Erro.Should().BeFalse(resultado.Texto);
    }

    private static string CaminhoDoSolidCraft()
    {
        var caminho = new ConfigurationBuilder()
            .AddUserSecrets("castwork-webapi")
            .AddEnvironmentVariables()
            .Build()[$"{SolidCraftOptions.Secao}:Caminho"];
        if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho))
        {
            Assert.Ignore("SolidCraft não configurado (SolidCraft:Caminho): teste com o SolidCraft ignorado.");
        }

        return caminho!;
    }
}
