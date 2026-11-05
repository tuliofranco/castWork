namespace Castwork.Infra.Auth.Jwt;

/// <summary>
/// Nomes das claims do access token. Curtos e estáveis: o front lê os mesmos nomes.
/// </summary>
public static class CastworkClaims
{
    public const string UsuarioId = "sub";
    public const string Nome = "nome";
    public const string Email = "email";

    /// <summary>
    /// Nome do ateliê (configuração única da instalação), para o cabeçalho do front.
    /// </summary>
    public const string AtelieNome = "atelieNome";

    /// <summary>
    /// Códigos das regras (<c>pecas.ler</c>...). Para o front esconder menu e botões; a API sempre revalida no servidor.
    /// </summary>
    public const string Permissoes = "permissoes";
    public const string PerfilId = "perfil_id";
    public const string Admin = "admin";
    public const string VersaoSeguranca = "versao_seguranca";
}
