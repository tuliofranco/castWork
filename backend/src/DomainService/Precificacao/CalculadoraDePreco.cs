using System.Globalization;

using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.ValueObjects;

namespace Castwork.DomainService.Precificacao;

/// <summary>
/// Calcula o preço de uma versão. Pura e determinística (mesma entrada, mesmo preço): sem banco, sem relógio, sem IA.
/// Nada é gravado; o cálculo é refeito a cada leitura e congelar o preço é papel da proposta.
/// </summary>
public static class CalculadoraDePreco
{
    /// <summary>
    /// A partir de quantos dias a cotação usada vira aviso (mesmo limite da tela de preços do ateliê).
    /// </summary>
    public const int DiasParaCotacaoAntiga = 15;

    public const int DiferencaDoPesoCasasDecimais = 1;

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static PrecoCalculado Calcular(EntradaDoPreco entrada)
    {
        ArgumentNullException.ThrowIfNull(entrada);
        var parametros = entrada.Parametros;
        var pendencias = new List<PendenciaDoPreco>();
        var avisos = new List<AvisoDoPreco>();

        var metais = entrada.Metais.Select(m => CustoDoMetal(m, entrada.Cotacoes, parametros.PerdaPercentual)).ToList();
        AvaliarMetais(entrada, metais, pendencias, avisos);
        AvaliarModelo(entrada.Modelo, pendencias, avisos);

        var composicao = entrada.Composicao
            .Select(p => new CustoDaPeca(p.PecaId, p.Nome, p.Quantidade, p.CustoUnitario, p.CustoUnitario is null ? Dinheiro.Zero : p.CustoUnitario * p.Quantidade))
            .ToList();
        pendencias.AddRange(composicao
            .Where(p => p.CustoUnitario is null)
            .Select(p => new PendenciaDoPreco(TipoDePendencia.PecaSemCusto, $"{p.Nome} está sem custo na biblioteca.", PecaId: p.PecaId)));

        var maoDeObra = CustoDaMaoDeObra(entrada.MaoDeObra, parametros.ValorHora);
        if (maoDeObra.Tipo == TipoMaoDeObra.PorHoras && parametros.ValorHora == Dinheiro.Zero)
        {
            pendencias.Add(new PendenciaDoPreco(TipoDePendencia.ValorHoraADefinir, "O valor da hora do projeto está a definir."));
        }

        var custoDosMetais = Somar(metais.Select(m => m.Custo));
        var custoDaComposicao = Somar(composicao.Select(p => p.Custo));
        var custoDosServicos = Somar(entrada.Servicos.Select(s => s.Valor));
        var custoTotal = custoDosMetais + custoDaComposicao + maoDeObra.Custo + custoDosServicos;

        var markup = entrada.MarkupDaVersao ?? parametros.Markup;
        var semArredondar = custoTotal * (markup * (1 + (parametros.TaxasPercentual / 100m)));

        return new PrecoCalculado(
            entrada.Modelo is null ? OrigemDoPeso.Manual : OrigemDoPeso.Modelo,
            metais,
            custoDosMetais,
            composicao,
            custoDaComposicao,
            maoDeObra,
            entrada.Servicos,
            custoDosServicos,
            custoTotal,
            parametros.PerdaPercentual,
            markup,
            MarkupDaVersao: entrada.MarkupDaVersao is not null,
            parametros.TaxasPercentual,
            parametros.ArredondarPara,
            semArredondar,
            parametros.Arredondar(semArredondar),
            pendencias.Count == 0 ? StatusDoPreco.Completo : StatusDoPreco.Incompleto,
            pendencias,
            avisos);
    }

    /// <summary>
    /// Peso a fundir = peso × (1 + perda), em miligramas; o custo usa esse peso já arredondado, como aparece na tela.
    /// </summary>
    private static CustoDoMetal CustoDoMetal(MetalUsado metal, IReadOnlyList<CotacaoUsada> cotacoes, decimal perdaPercentual)
    {
        var pesoAFundir = metal.PesoEstimado * (1 + (perdaPercentual / 100m));
        var cotacao = cotacoes.FirstOrDefault(c => c.Metal == metal.Metal);
        var custo = cotacao is null ? Dinheiro.Zero : Dinheiro.Criar(pesoAFundir.Gramas * cotacao.Valor.Valor * metal.Teor.Pureza);
        decimal? diferenca = metal.PesoReal is null
            ? null
            : Math.Round((metal.PesoReal.Gramas - metal.PesoEstimado.Gramas) / metal.PesoEstimado.Gramas * 100m, DiferencaDoPesoCasasDecimais, MidpointRounding.AwayFromZero);

        return new CustoDoMetal(
            metal.LigaId,
            metal.Liga,
            metal.Metal,
            metal.Teor,
            metal.LigaAtiva,
            metal.PesoEstimado,
            pesoAFundir,
            cotacao?.Valor,
            cotacao is null ? null : cotacao.Valor * metal.Teor.Pureza,
            custo,
            metal.PesoReal,
            diferenca);
    }

    private static void AvaliarMetais(EntradaDoPreco entrada, List<CustoDoMetal> metais, List<PendenciaDoPreco> pendencias, List<AvisoDoPreco> avisos)
    {
        if (entrada.Modelo is { Medido: false })
        {
            pendencias.Add(new PendenciaDoPreco(TipoDePendencia.ModeloNaoMedido, "Não foi possível medir o modelo 3D da versão: confira o arquivo e tente de novo."));
        }
        else if (!metais.Exists(m => m.PesoEstimado > Peso.Zero))
        {
            pendencias.Add(new PendenciaDoPreco(
                TipoDePendencia.SemMetalComPeso,
                entrada.Modelo is null ? "A versão não tem nenhum metal com peso." : "O modelo 3D não tem nenhum corpo de metal com peso."));
        }

        pendencias.AddRange(metais
            .Where(m => !m.LigaAtiva)
            .Select(m => new PendenciaDoPreco(TipoDePendencia.LigaDesativada, $"A liga {m.Liga} foi desativada: troque por outra.", LigaId: m.LigaId)));

        // Uma pendência ou aviso por metal, mesmo com duas ligas dele (ouro amarelo e branco).
        foreach (var metal in metais.Select(m => m.Metal).Distinct())
        {
            var cotacao = entrada.Cotacoes.FirstOrDefault(c => c.Metal == metal);
            if (cotacao is null)
            {
                pendencias.Add(new PendenciaDoPreco(TipoDePendencia.MetalSemCotacao, $"O projeto não tem cotação de {Nome(metal)}.", Metal: metal));
                continue;
            }

            var dias = (int)Math.Floor((entrada.Agora - cotacao.CotadaEm).TotalDays);
            if (dias > DiasParaCotacaoAntiga)
            {
                avisos.Add(new AvisoDoPreco(TipoDeAviso.CotacaoAntiga, $"A cotação de {Nome(metal)} tem {dias} dias: confira antes de orçar.", metal));
            }
        }
    }

    /// <summary>
    /// O material é obrigatório: corpo sem material deixa o preço incompleto. Corpos de metal sobrepostos e corpos abertos
    /// (os que contariam no peso) são avisos.
    /// </summary>
    private static void AvaliarModelo(ModeloMedido? modelo, List<PendenciaDoPreco> pendencias, List<AvisoDoPreco> avisos)
    {
        if (modelo is not { Medido: true })
        {
            return;
        }

        var semMaterial = modelo.Corpos.Where(c => c.Uso == UsoDoCorpo.SemMaterial).Select(c => c.Nome).ToList();
        if (semMaterial.Count > 0)
        {
            var quantos = semMaterial.Count == 1 ? "1 corpo" : $"{semMaterial.Count} corpos";
            pendencias.Add(new PendenciaDoPreco(
                TipoDePendencia.CorposSemMaterial,
                $"{quantos} sem material no modelo 3D: {string.Join(", ", semMaterial)}.",
                Corpos: semMaterial));
        }

        avisos.AddRange(modelo.SobreposicoesDeMetal().Select(s => new AvisoDoPreco(
            TipoDeAviso.CorposSobrepostos,
            $"{s.CorpoA} e {s.CorpoB} se sobrepõem ({s.VolumeMm3.ToString("0.##", PtBr)} mm³): a parte comum conta duas vezes no peso.",
            Corpos: [s.CorpoA, s.CorpoB])));

        avisos.AddRange(modelo.Corpos
            .Where(c => !c.Fechado && c.Uso is UsoDoCorpo.Metal or UsoDoCorpo.SemMaterial)
            .Select(c => new AvisoDoPreco(
                TipoDeAviso.CorpoAberto,
                $"O corpo {c.Nome} está aberto: o volume não é confiável e ele fica fora do peso.",
                Corpos: [c.Nome])));
    }

    private static CustoDaMaoDeObra CustoDaMaoDeObra(MaoDeObra? maoDeObra, Dinheiro valorHora)
        => maoDeObra?.Tipo switch
        {
            null => new CustoDaMaoDeObra(null, null, null, Dinheiro.Zero),
            TipoMaoDeObra.PorHoras => new CustoDaMaoDeObra(TipoMaoDeObra.PorHoras, maoDeObra.Horas, valorHora, maoDeObra.Custo(valorHora)),
            _ => new CustoDaMaoDeObra(maoDeObra.Tipo, null, null, maoDeObra.Custo(valorHora)),
        };

    private static Dinheiro Somar(IEnumerable<Dinheiro> valores) => valores.Aggregate(Dinheiro.Zero, (total, valor) => total + valor);

    private static string Nome(Metal metal) => metal switch
    {
        Metal.Ouro => "ouro",
        Metal.Prata => "prata",
        _ => metal.ToString(),
    };
}
