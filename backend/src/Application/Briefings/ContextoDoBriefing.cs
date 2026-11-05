using System.Globalization;
using System.Text.Json;

using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Atelie;
using Castwork.Domain.Entities.Briefings;
using Castwork.Domain.Entities.Clientes;
using Castwork.Domain.Entities.Projetos;

namespace Castwork.Application.Briefings;

/// <summary>
/// Monta o que a IA e o PDF precisam saber além do briefing: ateliê, projeto e cliente.
/// </summary>
internal static class ContextoDoBriefing
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static async Task<(string Atelie, string Projeto, string? Cliente)> NomesAsync(IUnitOfWork unitOfWork, long projetoId, CancellationToken cancellationToken)
    {
        var (projeto, cliente) = await ProjetoEClienteAsync(unitOfWork, projetoId, cancellationToken);
        var atelie = await unitOfWork.GetRepository<ConfiguracaoAtelie>().FindBy(c => c.Id == ConfiguracaoAtelie.IdUnico)
            .Select(c => c.Nome).SingleOrDefaultAsync(cancellationToken);
        return (atelie ?? "Ateliê", projeto, cliente);
    }

    public static async Task<DadosDocumentoBriefing> ParaDocumentoAsync(IUnitOfWork unitOfWork, Briefing briefing, DateOnly hoje, CancellationToken cancellationToken)
    {
        var (projeto, cliente) = await ProjetoEClienteAsync(unitOfWork, briefing.ProjetoId, cancellationToken);
        var atelie = await unitOfWork.GetRepository<ConfiguracaoAtelie>().FindBy(c => c.Id == ConfiguracaoAtelie.IdUnico)
            .Select(c => new { c.Nome, c.Assinatura, c.CorMarca, c.Logo, c.Telefone, c.Email, c.Instagram, c.Site, c.AvisoBriefing, Cidade = c.Endereco == null ? null : c.Endereco.Cidade + "/" + c.Endereco.Uf })
            .AsNoTracking().SingleAsync(cancellationToken);

        var contatos = new[] { atelie.Telefone, atelie.Email, atelie.Instagram is null ? null : "@" + atelie.Instagram, atelie.Site }
            .Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c!).ToList();
        var marca = new MarcaDoDocumento(atelie.Nome, atelie.Assinatura, atelie.CorMarca, atelie.Logo, atelie.Cidade, contatos);

        var especificacoes = new List<(string, string)> { ("Tipo de peça", briefing.TipoJoia.ToString()) };
        void Se(string rotulo, string? valor)
        {
            if (!string.IsNullOrWhiteSpace(valor))
            {
                especificacoes.Add((rotulo, valor));
            }
        }

        Se("Materiais", briefing.Materiais);
        Se("Medidas", briefing.Medidas);
        Se("Acabamento", briefing.Acabamento);
        Se("Pedras", briefing.Pedras);
        Se("Fecho", briefing.Fecho);
        Se("Ocasião", briefing.Ocasiao);

        return new DadosDocumentoBriefing(
            marca,
            briefing.Id,
            projeto,
            cliente,
            hoje,
            briefing.OQueSeraFeito ?? briefing.OQueOClienteQuer,
            especificacoes,
            briefing.Prazo?.ToString("dd/MM/yyyy", PtBr),
            Investimento(briefing.Orcamento),
            briefing.Observacoes,
            atelie.AvisoBriefing);
    }

    /// <summary>
    /// "4000" vira "R$ 4.000,00"; textos livres ("até R$ 3.000") ficam como o joalheiro escreveu.
    /// </summary>
    public static string? Investimento(string? orcamento)
    {
        if (string.IsNullOrWhiteSpace(orcamento))
        {
            return null;
        }

        var limpo = orcamento.Trim().Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return decimal.TryParse(limpo, NumberStyles.Number, PtBr, out var valor) ? valor.ToString("C", PtBr) : orcamento.Trim();
    }

    /// <summary>
    /// Estado atual do briefing, em JSON, com a data de hoje: a IA sempre vê o formulário mais recente,
    /// inclusive correções feitas depois de a conversa começar.
    /// </summary>
    public static string ParaIa(Briefing briefing, (string Atelie, string Projeto, string? Cliente) nomes, DateOnly hoje)
        => JsonSerializer.Serialize(new
        {
            hoje = hoje.ToString("dd/MM/yyyy", PtBr),
            atelie = nomes.Atelie,
            projeto = nomes.Projeto,
            cliente = nomes.Cliente ?? "(projeto autoral, sem cliente)",
            briefing = new
            {
                tipoJoia = briefing.TipoJoia.ToString(),
                oQueOClienteQuer = briefing.OQueOClienteQuer,
                ocasiao = briefing.Ocasiao,
                materiais = briefing.Materiais,
                medidas = briefing.Medidas,
                prazo = briefing.Prazo?.ToString("dd/MM/yyyy", PtBr),
                orcamento = briefing.Orcamento,
                oQueSeraFeito = briefing.OQueSeraFeito,
                acabamento = briefing.Acabamento,
                pedras = briefing.Pedras,
                fecho = briefing.Fecho,
                observacoes = briefing.Observacoes,
            },
        });

    private static async Task<(string Projeto, string? Cliente)> ProjetoEClienteAsync(IUnitOfWork unitOfWork, long projetoId, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().FindBy(p => p.Id == projetoId)
            .Select(p => new { p.Nome, p.ClienteId }).AsNoTracking().SingleAsync(cancellationToken);
        var cliente = projeto.ClienteId is null
            ? null
            : await unitOfWork.GetRepository<Cliente>().FindBy(c => c.Id == projeto.ClienteId).Select(c => c.Nome).SingleOrDefaultAsync(cancellationToken);
        return (projeto.Nome, cliente);
    }
}
