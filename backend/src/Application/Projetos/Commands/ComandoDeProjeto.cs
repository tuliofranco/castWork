namespace Castwork.Application.Projetos.Commands;

/// <summary>
/// Base dos commands que alteram um projeto existente. O Id vem da rota (<c>/projetos/{projetoId}</c>).
/// </summary>
public abstract class ComandoDeProjeto
{
    public long ProjetoId { get; set; }
}

/// <summary>
/// Commands sobre uma versão específica (<c>/projetos/{projetoId}/versoes/{versaoId}</c>).
/// </summary>
public abstract class ComandoDeVersao : ComandoDeProjeto
{
    public long VersaoId { get; set; }
}
