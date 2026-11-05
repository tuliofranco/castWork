using Castwork.Domain.Entities.Clientes;
using Castwork.Domain.Entities.Clientes.Eventos;
using Castwork.Domain.ValueObjects;

namespace Castwork.UnitTests.Domain.Entities.Clientes;

public class ClienteTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public void DeveCriarClienteAtivoNormalizandoDadosEPublicandoEvento()
    {
        var cliente = Cliente.Criar(" Maria Silva ", Documento.Criar("52998224725"), "Maria@Email.com ", null, null, null, Agora);

        cliente.Should().BeEquivalentTo(new { Nome = "Maria Silva", Email = "maria@email.com", Telefone = (string?)null, Ativo = true });
        cliente.Documento!.Tipo.Should().Be(TipoDocumento.Cpf);
        cliente.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ClienteCadastradoEvent>();
    }

    [Test]
    public void DeveDesativarEReativarCliente()
    {
        var cliente = Cliente.Criar("Maria", null, null, null, null, null, Agora);

        cliente.Desativar();
        var ativoDepoisDeDesativar = cliente.Ativo;
        cliente.Reativar();

        ativoDepoisDeDesativar.Should().BeFalse();
        cliente.Ativo.Should().BeTrue();
    }
}
