namespace Castwork.GraphQl;

/// <summary>
/// Policies usadas no <c>[Authorize]</c> dos resolvers: os códigos das regras do catálogo
/// (o Hosting registra uma policy por regra). Um teste garante que cada constante existe no catálogo.
/// </summary>
public static class Politicas
{
    public const string ClientesLer = "clientes.ler";
    public const string PecasLer = "pecas.ler";
    public const string ProjetosLer = "projetos.ler";
}
