using Mapster;

namespace Castwork.Application.Common.Mapping;

/// <summary>
/// O tipo que implementa é a origem do mapa para <typeparamref name="T"/> (ex.: query → filtro).
/// </summary>
public interface IMapTo<T>
{
    void Mapping(TypeAdapterConfig config) => MappingConvencoes.NovoMapa(config, GetType(), typeof(T));
}
