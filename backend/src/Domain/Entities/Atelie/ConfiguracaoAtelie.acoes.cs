using Castwork.Domain.Common;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Atelie;

/// <summary>
/// Ações da <see cref="ConfiguracaoAtelie"/>.
/// </summary>
public sealed partial class ConfiguracaoAtelie
{
    public void Atualizar(string nome, Documento? documento, string? email, string? telefone, Endereco? endereco, Dinheiro valorHoraPadrao)
    {
        Nome = Guard.Obrigatorio(nome, nameof(Nome), NomeMaxLength);
        Documento = documento;
        Email = Guard.Opcional(email, nameof(Email), EmailMaxLength)?.ToLowerInvariant();
        Telefone = Guard.Opcional(telefone, nameof(Telefone), TelefoneMaxLength);
        Endereco = endereco;
        ValorHoraPadrao = valorHoraPadrao;
    }

    public void DefinirLogo(string? chaveArquivo) => LogoArquivo = Guard.Opcional(chaveArquivo, nameof(LogoArquivo), LogoMaxLength);
}
