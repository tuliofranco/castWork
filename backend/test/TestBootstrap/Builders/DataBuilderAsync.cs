using Bogus;

using Castwork.TestBootstrap.Ambiente;

using MediatR;

namespace Castwork.TestBootstrap.Builders;

/// <summary>
/// Cria dados de teste executando o próprio command de produção (pelo MediatR), e não inserindo direto no banco:
/// o dado nasce pelas mesmas regras do sistema. Regras:
/// <list type="bullet">
/// <item>crie um builder novo só quando nenhum existente servir;</item>
/// <item>builders se compõem (o de peça usa o de cliente);</item>
/// <item><see cref="TearDownAsync"/> remove só o que este builder criou e delega aos compostos.</item>
/// </list>
/// </summary>
public abstract class DataBuilderAsync<TBuilder, TCommand> : IDataBuilder
    where TBuilder : DataBuilderAsync<TBuilder, TCommand>
    where TCommand : IRequest<long>
{
    private readonly List<Action<TCommand>> _customizacoes = [];

    protected static Faker Faker { get; } = new("pt_BR");

    protected List<long> IdsCriados { get; } = [];

    /// <summary>
    /// Ajusta o command padrão antes de enviar. Acumula: vale para todos os <see cref="BuildAsync"/> seguintes.
    /// </summary>
    public TBuilder CustomizeCommand(Action<TCommand> customizar)
    {
        _customizacoes.Add(customizar);
        return (TBuilder)this;
    }

    /// <summary>
    /// Monta o command (padrão + customizações) sem enviar. Útil para testar validators.
    /// </summary>
    public async Task<TCommand> BuildCommandAsync()
    {
        var command = await CriarCommandPadraoAsync();
        _customizacoes.ForEach(customizar => customizar(command));
        return command;
    }

    public async Task<long> BuildAsync() => await SendAsync(await BuildCommandAsync());

    /// <summary>
    /// Envia um command montado com <see cref="BuildCommandAsync"/> e registra o Id para o TearDown.
    /// Use no Act quando o teste é sobre o próprio command: enviar por fora do builder deixa lixo no banco.
    /// </summary>
    public async Task<long> SendAsync(TCommand command)
    {
        var id = await AmbienteDeTeste.SendAsync(command);
        IdsCriados.Add(id);
        return id;
    }

    public async Task TearDownAsync()
    {
        if (IdsCriados.Count > 0)
        {
            await RemoverAsync(IdsCriados);
            IdsCriados.Clear();
        }

        await TearDownCompostosAsync();
    }

    protected abstract Task<TCommand> CriarCommandPadraoAsync();

    protected abstract Task RemoverAsync(IReadOnlyCollection<long> ids);

    /// <summary>
    /// Sobrescreva para delegar o TearDown aos builders compostos (depois de remover os próprios dados).
    /// </summary>
    protected virtual Task TearDownCompostosAsync() => Task.CompletedTask;
}
