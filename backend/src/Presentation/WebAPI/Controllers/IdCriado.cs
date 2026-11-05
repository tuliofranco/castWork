namespace Castwork.WebAPI.Controllers;

/// <summary>
/// Resposta de criação: o Id do registro novo (a leitura completa vem pelo GraphQL).
/// </summary>
public sealed record IdCriado(long Id);
