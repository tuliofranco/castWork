using System.Reflection;
using System.Runtime.CompilerServices;

using Mapster;

namespace Castwork.Application.Common.Mapping;

/// <summary>
/// Monta a configuração do Mapster varrendo a Application atrás de <see cref="IMapFrom{T}"/> e <see cref="IMapTo{T}"/>.
/// </summary>
public static class MappingConfiguration
{
    public static TypeAdapterConfig Criar()
    {
        var config = new TypeAdapterConfig
        {
            // Todo membro do destino precisa ter origem: DTO com campo sem fonte quebra na inicialização
            // (Compile), e não aparece como null silencioso na tela.
            RequireDestinationMemberSource = true,
        };

        // Nome completo: dentro deste namespace, "AssemblyReference" resolveria para o de Application.Common.
        Registrar(config, typeof(Castwork.Application.AssemblyReference).Assembly);
        return config;
    }

    private static void Registrar(TypeAdapterConfig config, Assembly assembly)
    {
        var interfacesDeMapa = new[] { typeof(IMapFrom<>), typeof(IMapTo<>) };

        var tipos = assembly.GetExportedTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false })
            .Select(t => (Tipo: t, Interfaces: t.GetInterfaces()
                .Where(i => i.IsGenericType && interfacesDeMapa.Contains(i.GetGenericTypeDefinition()))
                .ToList()))
            .Where(x => x.Interfaces.Count > 0);

        foreach (var (tipo, interfaces) in tipos)
        {
            // Só precisa de uma instância para chamar Mapping; não executa o construtor do DTO.
            var instancia = RuntimeHelpers.GetUninitializedObject(tipo);
            foreach (var interfaceDeMapa in interfaces)
            {
                interfaceDeMapa.GetMethod(nameof(IMapFrom<object>.Mapping))!.Invoke(instancia, [config]);
            }
        }
    }
}
