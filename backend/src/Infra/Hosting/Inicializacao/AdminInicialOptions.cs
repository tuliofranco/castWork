namespace Castwork.Infra.Hosting.Inicializacao;

/// <summary>
/// Primeiro administrador, criado só quando a tabela de usuários está vazia (instalação nova).
/// Valores em User Secrets / variáveis de ambiente (<c>Acesso__AdminInicial__Senha</c>). Depois do primeiro
/// login, troque a senha e remova a configuração.
/// </summary>
public sealed class AdminInicialOptions
{
    public const string Secao = "Acesso:AdminInicial";

    public string? Nome { get; init; }

    public string? Email { get; init; }

    public string? Senha { get; init; }

    public bool Configurado => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Senha);
}
