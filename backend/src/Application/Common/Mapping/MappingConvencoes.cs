using System.Reflection;

using Mapster;

namespace Castwork.Application.Common.Mapping;

public static class MappingConvencoes
{
    /// <summary>
    /// Cria o mapa aplicando a convenção do time: se o destino tem <c>&lt;Origem&gt;Id</c> (ex.: <c>PecaId</c>)
    /// e a origem tem <c>Id</c>, os dois são ligados. O resto vem do Mapster, inclusive o flattening
    /// (<c>ClienteNome</c> ← <c>Cliente.Nome</c>).
    /// </summary>
    public static TypeAdapterSetter NovoMapa(TypeAdapterConfig config, Type origem, Type destino)
    {
        var setter = config.NewConfig(origem, destino);

        var idComNomeDaOrigem = $"{origem.Name}Id";
        if (destino.GetProperty(idComNomeDaOrigem, BindingFlags.Public | BindingFlags.Instance) is not null
            && origem.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance) is not null)
        {
            setter.Map(idComNomeDaOrigem, "Id");
        }

        return setter;
    }
}
