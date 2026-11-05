using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Acesso;
using Castwork.Domain.Entities.Atelie;

namespace Castwork.Application.Acesso.Common;

/// <summary>
/// Emite access token + refresh token para um usuário. Compartilhado por login, renovação e troca de senha.
/// Adiciona o refresh token ao UnitOfWork; quem chama faz o <c>SalvarAsync</c>.
/// </summary>
public sealed class EmissorDeSessao(IUnitOfWork unitOfWork, ITokenService tokenService, TimeProvider timeProvider)
{
    public const string MensagemSessaoInvalida = "Sessão expirada. Entre novamente.";

    public async Task<SessaoDto> EmitirAsync(Usuario usuario, Guid familia, CancellationToken cancellationToken)
    {
        var agora = timeProvider.GetUtcNow();
        var nomeDoAtelie = await unitOfWork.GetRepository<ConfiguracaoAtelie>()
            .FindBy(c => c.Id == ConfiguracaoAtelie.IdUnico)
            .Select(c => c.Nome)
            .SingleAsync(cancellationToken);
        var acesso = tokenService.GerarAccessToken(usuario, await PermissoesAsync(usuario, cancellationToken), nomeDoAtelie);
        var refresh = tokenService.GerarRefreshToken();
        var registro = RefreshToken.Emitir(
            usuario.Id,
            tokenService.CalcularHash(refresh),
            familia,
            usuario.VersaoSeguranca,
            agora,
            tokenService.ValidadeDoRefreshToken);

        unitOfWork.GetRepository<RefreshToken>().Adicionar(registro);

        return new SessaoDto(acesso.Token, acesso.ExpiraEm, refresh, registro.ExpiraEm, usuario.Id, usuario.Nome, usuario.IsAdmin, usuario.PerfilId);
    }

    /// <summary>
    /// Revoga todos os tokens ainda válidos da família (logout ou suspeita de roubo).
    /// </summary>
    public async Task RevogarFamiliaAsync(Guid familia, string motivo, CancellationToken cancellationToken)
    {
        var agora = timeProvider.GetUtcNow();
        var tokens = await unitOfWork.GetRepository<RefreshToken>()
            .FindBy(t => t.Familia == familia && t.RevogadoEm == null)
            .ToListAsync(cancellationToken);

        tokens.ForEach(t => t.Revogar(agora, motivo));
    }

    /// <summary>
    /// Admin tem todas as regras; os demais, as do perfil, traduzidas para os códigos do catálogo.
    /// </summary>
    private async Task<IReadOnlyCollection<string>> PermissoesAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        if (usuario.IsAdmin || usuario.PerfilId is null)
        {
            return usuario.IsAdmin ? CatalogoDeRegras.TodosOsCodigos() : [];
        }

        var regras = await unitOfWork.GetRepository<Perfil>()
            .FindBy(p => p.Id == usuario.PerfilId)
            .SelectMany(p => p.Regras.Select(r => r.RegraId))
            .ToListAsync(cancellationToken);

        return CatalogoDeRegras.Codigos(regras);
    }
}
