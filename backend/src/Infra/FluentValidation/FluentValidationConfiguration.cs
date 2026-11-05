using System.Globalization;
using System.Text.Json;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

namespace Castwork.Infra.FluentValidation;

public static class FluentValidationConfiguration
{
    public static IServiceCollection AddValidacao(this IServiceCollection services)
    {
        ConfigurarGlobal();
        return services.AddValidatorsFromAssembly(typeof(Application.AssemblyReference).Assembly);
    }

    /// <summary>
    /// Opções globais (estáticas) do FluentValidation. Também chamado pelos testes.
    /// </summary>
    public static void ConfigurarGlobal()
    {
        // Mensagens padrão em pt-BR, independentemente do idioma do servidor.
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.GetCultureInfo("pt-BR");

        // Primeira regra que falha encerra a cadeia daquela propriedade: sem erros em cascata
        // e sem ir ao banco (MustExists) quando o valor já é inválido.
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;

        // Nome do campo no erro em camelCase, igual ao JSON que o front envia (clienteId, prazoEntrega).
        ValidatorOptions.Global.PropertyNameResolver = (_, membro, expressao) =>
            membro is null ? null : JsonNamingPolicy.CamelCase.ConvertName(membro.Name);
    }
}
