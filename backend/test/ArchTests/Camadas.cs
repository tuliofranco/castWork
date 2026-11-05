using System.Reflection;

namespace Castwork.ArchTests;

/// <summary>
/// Assemblies e namespaces de cada camada, centralizados para os testes não repetirem strings.
/// </summary>
internal static class Camadas
{
    public const string Domain = "Castwork.Domain";
    public const string DomainService = "Castwork.DomainService";
    public const string Application = "Castwork.Application";
    public const string ApplicationCommon = "Castwork.Application.Common";
    public const string Infra = "Castwork.Infra";
    public const string Hosting = "Castwork.Infra.Hosting";
    public const string GraphQl = "Castwork.GraphQl";
    public const string WebApi = "Castwork.WebAPI";

    public static readonly Assembly DomainAssembly = typeof(Domain.AssemblyReference).Assembly;
    public static readonly Assembly DomainServiceAssembly = typeof(DomainService.AssemblyReference).Assembly;
    public static readonly Assembly ApplicationAssembly = typeof(Application.AssemblyReference).Assembly;
    public static readonly Assembly ApplicationCommonAssembly = typeof(Application.Common.AssemblyReference).Assembly;
    public static readonly Assembly GraphQlAssembly = typeof(GraphQl.AssemblyReference).Assembly;
    public static readonly Assembly WebApiAssembly = typeof(Program).Assembly;

    public static readonly Assembly[] InfraAssemblies =
    [
        typeof(Infra.Data.AssemblyReference).Assembly,
        typeof(Infra.Migrations.AssemblyReference).Assembly,
        typeof(Infra.Auth.AssemblyReference).Assembly,
        typeof(Infra.MediatR.AssemblyReference).Assembly,
        typeof(Infra.FluentValidation.AssemblyReference).Assembly,
    ];

    /// <summary>
    /// Bibliotecas de infraestrutura que o núcleo (Domain/DomainService/Application) não pode conhecer.
    /// </summary>
    public static readonly string[] FrameworksDeInfra =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "HotChocolate",
        "Hangfire",
        "Refit",
        "System.IdentityModel.Tokens.Jwt",
        "Npgsql",
        "AutoMapper",
    ];

    public static readonly string[] FrameworksDeInfraExcetoEfCore =
        [.. FrameworksDeInfra.Where(f => f != "Microsoft.EntityFrameworkCore")];

    /// <summary>
    /// Partes do EF Core que dão acesso direto ao banco. Só a Infra.Data pode usá-las.
    /// </summary>
    public static readonly string[] PartesProibidasDoEfCore =
    [
        "Microsoft.EntityFrameworkCore.DbContext",
        "Microsoft.EntityFrameworkCore.DbSet",
        "Microsoft.EntityFrameworkCore.ChangeTracking",
        "Microsoft.EntityFrameworkCore.Infrastructure",
        "Microsoft.EntityFrameworkCore.Metadata",
    ];
}
