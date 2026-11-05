namespace Castwork.Domain.Entities.Acesso;

/// <summary>
/// Regra (permissão) do catálogo do sistema. Não é criada pelo usuário: é sincronizada na inicialização
/// a partir do catálogo em código. O Id é o GUID estável da regra, por isso não herda de BaseEntity.
/// </summary>
public sealed class Regra
{
    public const int CodigoMaxLength = 50;
    public const int GrupoMaxLength = 50;
    public const int DescricaoMaxLength = 150;

    public Regra(Guid id, string codigo, string grupo, string descricao)
    {
        Id = id;
        Codigo = codigo;
        Grupo = grupo;
        Descricao = descricao;
    }

    private Regra()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>
    /// Código <c>&lt;dominio&gt;.&lt;acao&gt;</c> que vai no JWT.
    /// </summary>
    public string Codigo { get; private set; } = string.Empty;

    public string Grupo { get; private set; } = string.Empty;

    public string Descricao { get; private set; } = string.Empty;

    public void Atualizar(string codigo, string grupo, string descricao)
    {
        Codigo = codigo;
        Grupo = grupo;
        Descricao = descricao;
    }
}
