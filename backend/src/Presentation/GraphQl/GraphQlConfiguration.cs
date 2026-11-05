using Castwork.Application.Atelie.Queries.GetConfiguracaoAtelie;
using Castwork.Application.Common.Enderecos;
using Castwork.GraphQl.DataLoaders;
using Castwork.GraphQl.Erros;
using Castwork.GraphQl.Queries;
using Castwork.GraphQl.Types.Clientes;
using Castwork.GraphQl.Types.Pecas;

using HotChocolate.AspNetCore;

using Microsoft.Extensions.DependencyInjection;

namespace Castwork.GraphQl;

public static class GraphQlConfiguration
{
    public const int ProfundidadeMaxima = 8;
    public const int TamanhoMaximoDaPagina = 50;

    /// <summary>
    /// GraphQL para as leituras ricas das telas (listagens e detalhes); REST fica com commands e arquivos.
    /// Proteções: profundidade máxima, custo por requisição, página máxima, sem batching de operações,
    /// e introspecção/Nitro só em desenvolvimento.
    /// </summary>
    public static IServiceCollection AddGraphQlCastwork(this IServiceCollection services, bool desenvolvimento)
    {
        services.AddGraphQLServer()
            .AddAuthorization()
            .AddQueryType(d => d.Name(OperationTypeNames.Query))
            .AddTypeExtension<PecasQueries>()
            .AddTypeExtension<ClientesQueries>()
            .AddTypeExtension<AtelieQueries>()
            .AddType<PecaType>()
            .AddType<ClienteType>()

            // DTO da Application exposto com nome de domínio, sem o sufixo técnico.
            .AddType(new ObjectType<ConfiguracaoAtelieDto>(d => d.Name("ConfiguracaoAtelie")))
            .AddType(new ObjectType<EnderecoDto>(d =>
            {
                d.Name("Endereco");
                d.Ignore(e => e.ParaValueObject());
            }))

            // Comentários XML do C# são para quem mantém o código; a descrição pública fica nos [GraphQLDescription].
            .ModifyOptions(o => o.UseXmlDocumentation = false)
            .AddFiltering()
            .AddSorting()
            .AddDataLoader<ClientePorIdDataLoader>()
            .AddDataLoader<PecasPorClienteDataLoader>()
            .AddErrorFilter<ErrorFilter>()
            .ModifyPagingOptions(o =>
            {
                o.DefaultPageSize = 20;
                o.MaxPageSize = TamanhoMaximoDaPagina;
                o.IncludeTotalCount = true;
            })
            .ModifyRequestOptions(o =>
            {
                o.IncludeExceptionDetails = desenvolvimento;
                o.ExecutionTimeout = TimeSpan.FromSeconds(15);
            })
            .ModifyCostOptions(o => o.EnforceCostLimits = true)
            .AddMaxExecutionDepthRule(ProfundidadeMaxima, skipIntrospectionFields: true)
            .DisableIntrospection(!desenvolvimento)
            .ModifyServerOptions(o =>
            {
                o.Batching = AllowedBatching.None;
                o.Tool.Enable = desenvolvimento;
            });

        return services;
    }
}
