using Castwork.Domain.Common;
using Castwork.Domain.Entities.Clientes.Eventos;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Clientes;

/// <summary>
/// Ações de negócio do <see cref="Cliente"/>: únicas formas de criar e alterar o agregado.
/// O estado (propriedades e limites) fica em <c>Cliente.cs</c>.
/// </summary>
public sealed partial class Cliente
{
    public static Cliente Criar(
        string nome,
        Documento? documento,
        string? email,
        string? telefone,
        Endereco? endereco,
        string? observacoes,
        DateTimeOffset agora)
    {
        var cliente = new Cliente { Ativo = true };
        cliente.DefinirDados(nome, documento, email, telefone, endereco, observacoes);
        cliente.AdicionarEvento(new ClienteCadastradoEvent(cliente, agora));
        return cliente;
    }

    public void Atualizar(
        string nome,
        Documento? documento,
        string? email,
        string? telefone,
        Endereco? endereco,
        string? observacoes)
        => DefinirDados(nome, documento, email, telefone, endereco, observacoes);

    public void Desativar() => Ativo = false;

    public void Reativar() => Ativo = true;

    private void DefinirDados(
        string nome,
        Documento? documento,
        string? email,
        string? telefone,
        Endereco? endereco,
        string? observacoes)
    {
        Nome = Guard.Obrigatorio(nome, nameof(Nome), NomeMaxLength);
        Documento = documento;
        Email = Guard.Opcional(email, nameof(Email), EmailMaxLength)?.ToLowerInvariant();
        Telefone = Guard.Opcional(telefone, nameof(Telefone), TelefoneMaxLength);
        Endereco = endereco;
        Observacoes = Guard.Opcional(observacoes, nameof(Observacoes), ObservacoesMaxLength);
    }
}
