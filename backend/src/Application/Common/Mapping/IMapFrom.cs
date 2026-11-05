using Mapster;

namespace Castwork.Application.Common.Mapping;

/// <summary>
/// O tipo que implementa é o destino do mapa a partir de <typeparamref name="T"/> (entidade → DTO).
/// Sobrescreva <see cref="Mapping"/> só quando a convenção não bastar.
/// </summary>
public interface IMapFrom<T>
{
    void Mapping(TypeAdapterConfig config) => MappingConvencoes.NovoMapa(config, typeof(T), GetType());
}
