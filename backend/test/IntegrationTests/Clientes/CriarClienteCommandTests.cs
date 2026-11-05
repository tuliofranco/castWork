using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Clientes;
using Castwork.Domain.ValueObjects;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

namespace Castwork.IntegrationTests.Clientes;

public class CriarClienteCommandTests : TesteDeIntegracao
{
    [Test]
    public async Task DeveCriarClienteComEnderecoELerAgregadoCompletoAsync()
    {
        var clientes = Usar(new ClienteBuilder()).ComEndereco();
        var command = await clientes
            .CustomizeCommand(c => (c.Nome, c.Documento, c.Email) = ("Maria Silva", "529.982.247-25", "Maria@Email.com"))
            .BuildCommandAsync();

        var id = await clientes.SendAsync(command);

        // Carrega a entidade inteira (complex type Endereco incluso), o caso que o InMemory não suportava.
        var cliente = await AmbienteDeTeste.ExecutarNoEscopoAsync(sp =>
            sp.GetRequiredService<IUnitOfWork>().GetRepository<Cliente>().ObterPorIdOuFalharAsync(id, CancellationToken.None));
        cliente.Should().BeEquivalentTo(new
        {
            Nome = "Maria Silva",
            Documento = Documento.Criar("52998224725"),
            Email = "maria@email.com",
            Ativo = true,
            Endereco = new { Cep = "80010000", Bairro = "Centro", Cidade = "Curitiba", Uf = "PR", Complemento = (string?)null },
            CriadoEm = RelogioDeTeste.Inicio,
        });
    }

    [Test]
    public async Task DeveCriarClienteSemEnderecoComEnderecoNuloAsync()
    {
        var id = await Usar(new ClienteBuilder()).BuildAsync();

        var cliente = await AmbienteDeTeste.ExecutarNoEscopoAsync(sp =>
            sp.GetRequiredService<IUnitOfWork>().GetRepository<Cliente>().ObterPorIdOuFalharAsync(id, CancellationToken.None));

        cliente.Endereco.Should().BeNull();
    }

    [Test]
    public async Task DeveRejeitarDocumentoJaCadastradoMesmoComOutraMascaraAsync()
    {
        await Usar(new ClienteBuilder()).CustomizeCommand(c => c.Documento = "529.982.247-25").BuildAsync();
        var clientes = Usar(new ClienteBuilder()).CustomizeCommand(c => c.Documento = "52998224725");
        var command = await clientes.BuildCommandAsync();

        var enviar = () => clientes.SendAsync(command);

        (await enviar.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { PropertyName = "documento", ErrorMessage = "Já existe um cliente com este CPF/CNPJ." });
    }

    [Test]
    public async Task DeveRejeitarDocumentoEmailEEnderecoInvalidosAsync()
    {
        var clientes = Usar(new ClienteBuilder()).ComEndereco()
            .CustomizeCommand(c =>
            {
                c.Documento = "111.111.111-11";
                c.Email = "sem-arroba";
                c.Endereco!.Uf = "XX";
            });
        var command = await clientes.BuildCommandAsync();

        var enviar = () => clientes.SendAsync(command);

        (await enviar.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => (e.PropertyName, e.ErrorMessage)).Should().BeEquivalentTo(new[]
        {
            ("documento", "CPF/CNPJ inválido."),
            ("email", "'E-mail' é um endereço de email inválido."),
            ("endereco.uf", "UF inválida."),
        });
    }
}
