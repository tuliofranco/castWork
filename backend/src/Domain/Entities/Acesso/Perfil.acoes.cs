using Castwork.Domain.Common;

namespace Castwork.Domain.Entities.Acesso;

/// <summary>
/// Ações do <see cref="Perfil"/>.
/// </summary>
public sealed partial class Perfil
{
    public static Perfil Criar(string nome, IEnumerable<Guid> regras)
    {
        var perfil = new Perfil();
        perfil.Renomear(nome);
        perfil.DefinirRegras(regras);
        return perfil;
    }

    public void Renomear(string nome) => Nome = Guard.Obrigatorio(nome, nameof(Nome), NomeMaxLength);

    /// <summary>
    /// Substitui as regras do perfil, mexendo só no que mudou (mantém as linhas das regras que continuam).
    /// </summary>
    public void DefinirRegras(IEnumerable<Guid> regras)
    {
        var desejadas = regras.ToHashSet();

        _regras.RemoveAll(r => !desejadas.Contains(r.RegraId));

        var atuais = _regras.Select(r => r.RegraId).ToHashSet();
        _regras.AddRange(desejadas.Where(r => !atuais.Contains(r)).Select(r => new PerfilRegra(r)));
    }
}
