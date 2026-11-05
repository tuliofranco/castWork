using System.Collections.Frozen;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Castwork.Infra.Data.Converters;

/// <summary>
/// Grava um enum como um código de 1 caractere legível no banco ('B' = Briefing), em vez do número.
/// O mapa é explícito: renumerar ou renomear o enum no domínio não altera os dados gravados.
/// </summary>
public abstract class EnumCharConverter<TEnum>(CodigosDeEnum<TEnum> codigos)
    : ValueConverter<TEnum, string>(valor => codigos.Codificar(valor), codigo => codigos.Decodificar(codigo))
    where TEnum : struct, Enum;

/// <summary>
/// Mapa enum ↔ código nos dois sentidos, montado uma vez. Falha na criação se faltar código para algum
/// valor do enum ou se dois valores compartilharem o mesmo código.
/// </summary>
public sealed class CodigosDeEnum<TEnum>
    where TEnum : struct, Enum
{
    private readonly FrozenDictionary<TEnum, string> _paraCodigo;
    private readonly FrozenDictionary<string, TEnum> _paraEnum;

    public CodigosDeEnum(IReadOnlyDictionary<TEnum, char> mapa)
    {
        var semCodigo = Enum.GetValues<TEnum>().Where(v => !mapa.ContainsKey(v)).ToList();
        if (semCodigo.Count > 0)
        {
            throw new InvalidOperationException($"{typeof(TEnum).Name} sem código de banco: {string.Join(", ", semCodigo)}.");
        }

        if (mapa.Values.Distinct().Count() != mapa.Count)
        {
            throw new InvalidOperationException($"{typeof(TEnum).Name} tem códigos de banco repetidos.");
        }

        _paraCodigo = mapa.ToFrozenDictionary(kv => kv.Key, kv => kv.Value.ToString());
        _paraEnum = mapa.ToFrozenDictionary(kv => kv.Value.ToString(), kv => kv.Key);
    }

    public IReadOnlyDictionary<TEnum, string> Todos => _paraCodigo;

    public string Codificar(TEnum valor) => _paraCodigo[valor];

    public TEnum Decodificar(string codigo)
        => _paraEnum.TryGetValue(codigo, out var valor)
            ? valor
            : throw new InvalidOperationException($"Código '{codigo}' desconhecido para {typeof(TEnum).Name}.");
}
