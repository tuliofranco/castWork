using Castwork.GraphQl;
using Castwork.Infra.Hosting;
using Castwork.Infra.Hosting.Inicializacao;
using Castwork.WebAPI.Configuracao;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.AddServerHeader = false;
    kestrel.Limits.MaxRequestBodySize = 1 * 1024 * 1024;
    kestrel.ConfigureHttpsDefaults(https => https.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13);
});

builder.Services.AddCastworkInfra(builder.Configuration);
builder.Services.AddApiHttp(builder.Configuration, builder.Environment);
builder.Services.AddGraphQlCastwork(builder.Environment.IsDevelopment());

var app = builder.Build();

// Antes de aceitar requisições: se a migration falhar, a API não sobe com schema inconsistente.
await app.Services.InicializarCastworkAsync(app.Lifetime.ApplicationStopping);

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseHeadersDeSeguranca();
app.UseCors(ApiExtensions.PoliticaCors);
app.UseRateLimiter();
app.UseAuthentication();
app.UseRejeitarTokenInvalido();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(options => options.WithTitle("Castwork API")).AllowAnonymous();
}

app.MapGet("/health", () => Results.Ok("Castwork API")).AllowAnonymous();
app.MapControllers();

// A autorização do GraphQL é por campo ([Authorize] em todo resolver, garantido por teste),
// por isso o endpoint em si não exige token: o Nitro (dev) e a introspecção (dev) abrem sem login.
app.MapGraphQL().AllowAnonymous();

await app.RunAsync();

/// <summary>
/// Exposto para o WebApplicationFactory dos testes de integração.
/// </summary>
public partial class Program
{
}
