using System.Text.RegularExpressions;

using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;
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

    /// <summary>
    /// Identidade visual usada nos documentos enviados ao cliente.
    /// </summary>
    public void AtualizarMarca(string? assinatura, string? corMarca, string? instagram, string? site, string? avisoBriefing)
    {
        Assinatura = Guard.Opcional(assinatura, nameof(Assinatura), AssinaturaMaxLength);
        CorMarca = string.IsNullOrWhiteSpace(corMarca)
            ? CorPadrao
            : CorHexadecimal().IsMatch(corMarca.Trim()) ? corMarca.Trim().ToUpperInvariant() : throw new ValorInvalidoException(nameof(CorMarca), "deve estar no formato #RRGGBB");
        Instagram = Guard.Opcional(instagram?.Trim().TrimStart('@'), nameof(Instagram), InstagramMaxLength);
        Site = Guard.Opcional(site, nameof(Site), SiteMaxLength);
        AvisoBriefing = Guard.Opcional(avisoBriefing, nameof(AvisoBriefing), AvisoMaxLength);
    }

    /// <summary>
    /// Troca o logo. Só PNG e JPEG, conferidos pelo conteúdo do arquivo e não pela extensão.
    /// </summary>
    public void DefinirLogo(byte[] conteudo)
    {
        if (conteudo.Length == 0 || conteudo.Length > LogoTamanhoMaximo)
        {
            throw new ValorInvalidoException(nameof(Logo), $"deve ter até {LogoTamanhoMaximo / 1024} KB");
        }

        LogoTipo = TipoDaImagem(conteudo) ?? throw new ValorInvalidoException(nameof(Logo), "deve ser uma imagem PNG ou JPEG");
        Logo = conteudo;
    }

    public void RemoverLogo()
    {
        Logo = null;
        LogoTipo = null;
    }

    private static string? TipoDaImagem(byte[] conteudo) => conteudo switch
    {
        [0x89, 0x50, 0x4E, 0x47, ..] => "image/png",
        [0xFF, 0xD8, 0xFF, ..] => "image/jpeg",
        _ => null,
    };

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex CorHexadecimal();
}
