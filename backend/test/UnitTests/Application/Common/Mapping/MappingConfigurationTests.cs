using Castwork.Application.Common.Mapping;

namespace Castwork.UnitTests.Application.Common.Mapping;

public class MappingConfigurationTests
{
    [Test]
    public void DeveRegistrarUmMapaParaCadaTipoQueImplementaIMapFromOuIMapTo()
    {
        var esperados = typeof(Castwork.Application.AssemblyReference).Assembly.GetExportedTypes()
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IMapFrom<>) || i.GetGenericTypeDefinition() == typeof(IMapTo<>)))
                .Select(i => i.GetGenericTypeDefinition() == typeof(IMapFrom<>)
                    ? (Origem: i.GetGenericArguments()[0], Destino: t)
                    : (Origem: t, Destino: i.GetGenericArguments()[0])))
            .ToList();

        var registrados = MappingConfiguration.Criar().RuleMap.Keys.Select(k => (Origem: k.Source, Destino: k.Destination));

        esperados.Should().NotBeEmpty();
        registrados.Should().Contain(esperados, "um mapa não registrado cai na convenção do Mapster e ignora a configuração do DTO");
    }

    [Test]
    public void DeveCompilarMapeamentosEProjecoesDeTodosOsMapasRegistrados()
    {
        var config = MappingConfiguration.Criar();

        var compilar = () =>
        {
            config.Compile();
            foreach (var mapa in config.RuleMap.Keys)
            {
                config.CompileProjection(mapa.Source, mapa.Destination);
            }
        };

        compilar.Should().NotThrow("todo membro de DTO precisa ter origem (RequireDestinationMemberSource), também na projeção para SQL");
    }
}
