namespace Castwork.WebAPI.Controllers.Ferramentas;

/// <summary>
/// O arquivo que virou o atual depois de restaurar; nulo quando é o desenho em branco.
/// </summary>
public sealed record ArquivoRestaurado(long? ArquivoId);
