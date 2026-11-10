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

/// <summary>
/// Commands sobre uma peça do projeto (<c>/projetos/{projetoId}/pecas/{pecaDoProjetoId}</c>). A existência da peça fica com
/// o agregado (não encontrada, 404), como a da versão.
/// </summary>
public abstract class ComandoDePecaDoProjeto : ComandoDeProjeto
{
    public long PecaDoProjetoId { get; set; }
}
