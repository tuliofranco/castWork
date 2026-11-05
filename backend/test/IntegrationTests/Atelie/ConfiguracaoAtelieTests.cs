using Castwork.Application.Atelie.Commands.AtualizarConfiguracaoAtelie;
using Castwork.Application.Atelie.Queries.GetConfiguracaoAtelie;
using Castwork.Application.Common.Enderecos;
using Castwork.Application.Common.Exceptions;
using Castwork.Domain.Entities.Atelie;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

namespace Castwork.IntegrationTests.Atelie;

public class ConfiguracaoAtelieTests : TesteDeIntegracao
{
    [Test]
    public async Task DeveTerConfiguracaoPadraoCriadaPelaMigrationAsync()
    {
        var configuracao = await AmbienteDeTeste.SendAsync(new GetConfiguracaoAtelieQuery());

        configuracao.Should().BeEquivalentTo(new ConfiguracaoAtelieDto
        {
            Nome = "Meu ateliê",
            ValorHoraPadrao = 0m,
            PossuiLogo = false,
            CorMarca = ConfiguracaoAtelie.CorPadrao,
            AvisoBriefing = ConfiguracaoAtelie.AvisoPadrao,
        });
    }

    [Test]
    public async Task DeveAtualizarConfiguracaoEDevolverDocumentoFormatadoAsync()
    {
        var endereco = new EnderecoDto { Cep = "80010-000", Logradouro = "Rua XV", Numero = "10", Bairro = "Centro", Cidade = "Curitiba", Uf = "pr" };

        await AmbienteDeTeste.SendAsync(new AtualizarConfiguracaoAtelieCommand
        {
            Nome = "Ateliê Lima",
            Documento = "11222333000181",
            Email = "Contato@AtelieLima.com",
            ValorHoraPadrao = 85.5m,
            Endereco = endereco,
        });

        var configuracao = await AmbienteDeTeste.SendAsync(new GetConfiguracaoAtelieQuery());
        configuracao.Should().BeEquivalentTo(new
        {
            Nome = "Ateliê Lima",
            Documento = "11.222.333/0001-81",
            Email = "contato@atelielima.com",
            ValorHoraPadrao = 85.5m,
            Endereco = new { Cep = "80010000", Uf = "PR", Cidade = "Curitiba" },
        });
    }

    [Test]
    public async Task DeveImpedirAlteracaoPorUsuarioSemARegraMasPermitirLeituraAsync()
    {
        var perfil = await Usar(new PerfilBuilder()).BuildAsync();
        AmbienteDeTeste.Sessao.IsAdmin = false;
        AmbienteDeTeste.Sessao.PerfilId = perfil;

        var leitura = await AmbienteDeTeste.SendAsync(new GetConfiguracaoAtelieQuery());
        var alterar = () => AmbienteDeTeste.SendAsync(new AtualizarConfiguracaoAtelieCommand { Nome = "Outro" });

        leitura.Nome.Should().Be("Meu ateliê");
        await alterar.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
