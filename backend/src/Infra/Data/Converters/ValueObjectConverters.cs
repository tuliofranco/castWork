using Castwork.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Castwork.Infra.Data.Converters;

// Value objects de um único valor viram uma coluna. A leitura passa pela fábrica do VO,
// então um dado corrompido no banco falha ao carregar em vez de circular inválido pelo domínio.
public sealed class DinheiroConverter() : ValueConverter<Dinheiro, decimal>(d => d.Valor, v => Dinheiro.Criar(v));

public sealed class PesoConverter() : ValueConverter<Peso, decimal>(p => p.Gramas, v => Peso.EmGramas(v));

public sealed class TeorMetalConverter() : ValueConverter<TeorMetal, int>(t => t.Milesimos, v => TeorMetal.EmMilesimos(v));

public sealed class DocumentoConverter() : ValueConverter<Documento, string>(d => d.Numero, v => Documento.Criar(v));
