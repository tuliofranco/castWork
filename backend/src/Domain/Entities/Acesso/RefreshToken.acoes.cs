using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Acesso;

/// <summary>
/// Ações do <see cref="RefreshToken"/>.
/// </summary>
public sealed partial class RefreshToken
{
    public static RefreshToken Emitir(
        long usuarioId,
        string tokenHash,
        Guid familia,
        Guid versaoSegurancaDoUsuario,
        DateTimeOffset agora,
        TimeSpan validade)
        => tokenHash.Length != TokenHashLength
            ? throw new ValorInvalidoException(nameof(TokenHash), $"deve ter {TokenHashLength} caracteres")
            : new RefreshToken
            {
                UsuarioId = usuarioId,
                TokenHash = tokenHash,
                Familia = familia,
                VersaoSegurancaDoUsuario = versaoSegurancaDoUsuario,
                CriadoEm = agora,
                ExpiraEm = agora + validade,
            };

    public bool EstaAtivo(DateTimeOffset agora) => !FoiUsadoOuRevogado && ExpiraEm > agora;

    public void MarcarComoUsado(DateTimeOffset agora)
    {
        if (!EstaAtivo(agora))
        {
            throw new RegraDeNegocioException("Refresh token inválido, expirado ou já utilizado.");
        }

        UsadoEm = agora;
    }

    public void Revogar(DateTimeOffset agora, string motivo)
    {
        if (RevogadoEm is not null)
        {
            return;
        }

        RevogadoEm = agora;
        MotivoRevogacao = motivo.Length > MotivoMaxLength ? motivo[..MotivoMaxLength] : motivo;
    }
}
