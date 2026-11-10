using Castwork.Application.Abstractions;
using Castwork.Application.Ferramentas.Modelo3d;
using Castwork.Infra.Auth.Sessao;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Castwork.TestBootstrap.Ambiente;

/// <summary>
/// Sobe a API de verdade (o mesmo Program.cs), trocando só o banco (container), o relógio, o usuário logado
/// e os serviços externos do briefing (IA e gerador de PDF), a prévia e o medidor do modelo 3D.
/// As migrations rodam no startup, como em produção.
/// </summary>
public sealed class CastworkWebApplicationFactory(
    string connectionString,
    RelogioDeTeste relogio,
    SessaoDeTeste sessao,
    MedidorDoModeloDeTeste medidor,
    IReadOnlyDictionary<string, string>? configuracaoExtra = null)
    : WebApplicationFactory<Program>
{
    /// <summary>
    /// Chave aleatória por execução: nenhum segredo de teste fica versionado.
    /// </summary>
    public static readonly string ChaveJwt = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testes");
        builder.UseSetting("ConnectionStrings:Castwork", connectionString);
        builder.UseSetting("ConnectionStrings:CastworkMigrations", connectionString);
        builder.UseSetting("Jwt:ChaveAssinatura", ChaveJwt);

        // Todos os testes compartilham o IP do TestServer: limite alto para o rate limit não interferir.
        builder.UseSetting("LimitesDeRequisicao:AutenticacaoPorMinuto", "1000");
        foreach (var (chave, valor) in configuracaoExtra ?? new Dictionary<string, string>())
        {
            builder.UseSetting(chave, valor);
        }

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(relogio);

            // Serviços externos (IA paga, binários do WordCraft e do CADCraft) ficam de fora dos testes.
            services.RemoveAll<IGeradorDocumentoBriefing>();
            services.AddScoped<IGeradorDocumentoBriefing, GeradorDocumentoBriefingDeTeste>();
            services.RemoveAll<IAssistenteFerramenta>();
            services.AddScoped<IAssistenteFerramenta, AssistenteFerramentaDeTeste>();
            services.RemoveAll<IPreviaFerramenta>();
            services.AddSingleton<IPreviaFerramenta, PreviaFerramentaDeTeste>();
            services.RemoveAll<IMedidorDoModelo>();
            services.AddSingleton<IMedidorDoModelo>(medidor);

            // Requisição HTTP autenticada: sessão real, lida do JWT. Fora disso (SendAsync): sessão de teste.
            services.RemoveAll<ISessaoService>();
            services.AddScoped<ISessaoService>(sp =>
                sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User.Identity?.IsAuthenticated == true
                    ? ActivatorUtilities.CreateInstance<SessaoService>(sp)
                    : sessao);
        });
    }
}
