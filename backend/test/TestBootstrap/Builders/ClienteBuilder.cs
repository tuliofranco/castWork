using Bogus;
using Bogus.Extensions.Brazil;

using Castwork.Application.Clientes.Commands.CriarCliente;
using Castwork.Application.Common.Enderecos;
using Castwork.TestBootstrap.Ambiente;

using Microsoft.EntityFrameworkCore;

namespace Castwork.TestBootstrap.Builders;

public sealed class ClienteBuilder : DataBuilderAsync<ClienteBuilder, CriarClienteCommand>
{
    public ClienteBuilder ComEndereco()
        => CustomizeCommand(c => c.Endereco = new EnderecoDto
        {
            Cep = "80010-000",
            Logradouro = Faker.Address.StreetName(),
            Numero = Faker.Random.Number(1, 9999).ToString(System.Globalization.CultureInfo.InvariantCulture),
            Bairro = "Centro",
            Cidade = "Curitiba",
            Uf = "PR",
        });

    protected override Task<CriarClienteCommand> CriarCommandPadraoAsync()
    {
        // Pessoa nova a cada command: o Faker.Person é fixo por instância de Faker.
        var pessoa = new Person("pt_BR");
        return Task.FromResult(new CriarClienteCommand
        {
            Nome = pessoa.FullName,
            Documento = pessoa.Cpf(),
            Email = pessoa.Email,
            Telefone = Faker.Phone.PhoneNumber("(41) 9####-####"),
        });
    }

    protected override Task RemoverAsync(IReadOnlyCollection<long> ids)
        => AmbienteDeTeste.ExecutarNoBancoAsync(context => context.Clientes.Where(c => ids.Contains(c.Id)).ExecuteDeleteAsync());
}
