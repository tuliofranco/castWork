using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Acesso;

/// <summary>
/// Ações do <see cref="Usuario"/>: cadastro, controle de tentativas de login e mudanças que exigem
/// novo login (senha, perfil, desativação).
/// </summary>
public sealed partial class Usuario
{
    public static Usuario Criar(string nome, string email, string senhaHash, long? perfilId, bool isAdmin)
    {
        var usuario = new Usuario
        {
            Nome = Guard.Obrigatorio(nome, nameof(Nome), NomeMaxLength),
            Email = NormalizarEmail(email),
            SenhaHash = Guard.Obrigatorio(senhaHash, nameof(SenhaHash), SenhaHashMaxLength),
            Ativo = true,
            VersaoSeguranca = Guid.NewGuid(),
        };

        usuario.DefinirPerfil(perfilId, isAdmin);
        return usuario;
    }

    /// <summary>
    /// Mesma normalização usada para gravar e para buscar no login.
    /// </summary>
    public static string NormalizarEmail(string email)
        => Guard.Obrigatorio(email, nameof(Email), EmailMaxLength).ToLowerInvariant();

    public bool EstaBloqueado(DateTimeOffset agora) => BloqueadoAte > agora;

    /// <summary>
    /// A cada <see cref="MaximoDeTentativas"/> falhas seguidas, bloqueia por <see cref="DuracaoDoBloqueio"/>.
    /// </summary>
    public void RegistrarFalhaDeLogin(DateTimeOffset agora)
    {
        TentativasFalhas++;
        if (TentativasFalhas >= MaximoDeTentativas)
        {
            BloqueadoAte = agora + DuracaoDoBloqueio;
            TentativasFalhas = 0;
        }
    }

    public void RegistrarLogin(DateTimeOffset agora)
    {
        TentativasFalhas = 0;
        BloqueadoAte = null;
        UltimoLoginEm = agora;
    }

    /// <summary>
    /// Troca de senha pelo usuário: sessões abertas em outros dispositivos caem no próximo refresh.
    /// </summary>
    public void AlterarSenha(string novoHash)
    {
        SenhaHash = Guard.Obrigatorio(novoHash, nameof(SenhaHash), SenhaHashMaxLength);
        VersaoSeguranca = Guid.NewGuid();
    }

    /// <summary>
    /// Reprocessamento do hash com parâmetros mais fortes, feito no login. A senha é a mesma: não derruba sessões.
    /// </summary>
    public void AtualizarHashDaSenha(string novoHash)
        => SenhaHash = Guard.Obrigatorio(novoHash, nameof(SenhaHash), SenhaHashMaxLength);

    public void AlterarPerfil(long? perfilId, bool isAdmin)
    {
        DefinirPerfil(perfilId, isAdmin);
        VersaoSeguranca = Guid.NewGuid();
    }

    public void Desativar()
    {
        Ativo = false;
        VersaoSeguranca = Guid.NewGuid();
    }

    public void Reativar() => Ativo = true;

    private void DefinirPerfil(long? perfilId, bool isAdmin)
    {
        if (!isAdmin && perfilId is null)
        {
            throw new RegraDeNegocioException("Usuário que não é administrador precisa de um perfil.");
        }

        PerfilId = perfilId;
        IsAdmin = isAdmin;
    }
}
