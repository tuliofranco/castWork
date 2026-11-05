namespace Castwork.Application.Common.Tempo;

public static class TimeProviderExtensions
{
    private static readonly TimeZoneInfo Brasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    /// <summary>
    /// Data de hoje no fuso do ateliê. Não usa o fuso do servidor, que em container costuma ser UTC
    /// (e às 21h em Brasília já seria "amanhã").
    /// </summary>
    public static DateOnly HojeNoBrasil(this TimeProvider timeProvider)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), Brasilia).DateTime);
}
