namespace Castwork.TestBootstrap.Ambiente;

/// <summary>
/// Relógio controlado pelo teste. Diferente do FakeTimeProvider, pode voltar no tempo,
/// o que é necessário porque o mesmo relógio é reiniciado entre os testes.
/// </summary>
public sealed class RelogioDeTeste : TimeProvider
{
    public static readonly DateTimeOffset Inicio = new(2026, 11, 5, 13, 0, 0, TimeSpan.Zero);

    public DateTimeOffset Agora { get; set; } = Inicio;

    public override DateTimeOffset GetUtcNow() => Agora;

    public void Avancar(TimeSpan intervalo) => Agora += intervalo;

    public void Reiniciar() => Agora = Inicio;
}
