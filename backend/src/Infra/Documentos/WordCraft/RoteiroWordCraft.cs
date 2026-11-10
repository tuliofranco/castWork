using Castwork.Application.Abstractions;

namespace Castwork.Infra.Documentos.WordCraft;

/// <summary>
/// O visual comum dos documentos do ateliê (briefing e ficha técnica): fontes da marca, página A4, cabeçalho com a marca,
/// seções em versalete na cor do ateliê e tabelas. Monta, comando a comando, o roteiro que o
/// <c>wordcraft-cli run --script</c> executa. Os textos entram só como parâmetro de <c>text.insert</c>, nunca como comando.
/// </summary>
internal abstract class RoteiroWordCraft(string cor)
{
    protected const string Tinta = "2A2622";
    protected const string Suave = "6B645C";
    protected const string Linha = "E3DDD3";
    protected const string Serifada = "Fraunces";
    protected const string SemSerifa = "Instrument Sans";

    /// <summary>
    /// Fontes da marca, lidas uma vez dos recursos do assembly.
    /// </summary>
    private static readonly Lazy<IReadOnlyList<(string Nome, byte[] Conteudo)>> Fontes = new(() =>
    {
        var assembly = typeof(RoteiroWordCraft).Assembly;
        return assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("Fontes.", StringComparison.Ordinal))
            .Select(n =>
            {
                using var recurso = assembly.GetManifestResourceStream(n)!;
                using var memoria = new MemoryStream();
                recurso.CopyTo(memoria);
                return (n["Fontes.".Length..], memoria.ToArray());
            })
            .ToList();
    });

    protected List<object> Comandos { get; } = [];

    /// <summary>
    /// A cor da marca do ateliê, sem o <c>#</c>.
    /// </summary>
    protected string Cor { get; } = cor.TrimStart('#');

    protected static Celula Rotulo(string texto, double? tamanho = null) => new(texto, Tamanho: tamanho, Cor: Suave);

    /// <summary>
    /// Fontes, página e estilo base. Vem antes de qualquer texto.
    /// </summary>
    protected void Inicio()
    {
        foreach (var (nome, conteudo) in Fontes.Value)
        {
            Cmd("font.add", new { data = Convert.ToBase64String(conteudo), name = nome });
        }

        Cmd("layout.size", new { name = "A4" });
        Cmd("layout.margins", new { top = 48, bottom = 56, left = 56, right = 56 });
        Cmd("styles.modify", new { style = "Normal", chr = new { font = SemSerifa, size = 10.5, color = Tinta }, para = new { spaceAfter = 4 } });
    }

    /// <summary>
    /// Logo e nome do ateliê, com a assinatura e a cidade embaixo e um traço na cor da marca.
    /// </summary>
    protected void Cabecalho(MarcaDoDocumento marca)
    {
        if (marca.Logo is { Length: > 0 } logo)
        {
            Cmd("insert.picture", new { data = Convert.ToBase64String(logo), width = 40, alt = $"Logo {marca.Nome}" });
            Texto("   ");
        }

        Texto(marca.Nome, fonte: Serifada, tamanho: 18);
        Paragrafo();

        var linha = string.Join(" · ", new[] { marca.Assinatura, marca.Cidade }.Where(t => !string.IsNullOrWhiteSpace(t)));
        Texto(string.IsNullOrEmpty(linha) ? " " : linha, tamanho: 9, cor: Suave);
        Cmd("para.borders", new { kind = "bottom", width = 0.75, color = Cor });
        Cmd("para.spacing", new { after = 18 });
        Paragrafo();
        Cmd("para.borders", new { kind = "none" });
    }

    /// <summary>
    /// Os contatos do ateliê no rodapé (ou o nome, se não há contato).
    /// </summary>
    protected void Rodape(MarcaDoDocumento marca)
        => Cmd("insert.footer", new { text = string.Join("  ·  ", marca.Contatos.DefaultIfEmpty(marca.Nome)) });

    protected void Secao(string titulo)
    {
        Texto(titulo.ToUpperInvariant(), tamanho: 8, cor: Cor, negrito: true, espacamento: 1.6);
        Cmd("para.spacing", new { before = 14, after = 6 });
        Cmd("para.keepNext", new { value = true });
        Paragrafo();
    }

    /// <summary>
    /// Escreve as linhas separadas por tabulação e converte o trecho em tabela: o WordCraft não navega
    /// entre células por comando, mas converte texto em tabela de forma estável. Com <c>ajusteAoConteudo</c>
    /// as colunas seguem o texto (especificações); sem ele, dividem a largura da página.
    /// </summary>
    protected void Tabela(List<Celula[]> linhas, bool comLinhas, bool ajusteAoConteudo = false)
    {
        for (var i = 0; i < linhas.Count; i++)
        {
            for (var j = 0; j < linhas[i].Length; j++)
            {
                if (j > 0)
                {
                    Cmd("text.insertTab");
                }

                var celula = linhas[i][j];
                Texto(celula.Texto, fonte: celula.Fonte, tamanho: celula.Tamanho, cor: celula.Cor);
            }

            if (i < linhas.Count - 1)
            {
                Paragrafo();
            }
        }

        for (var i = 0; i < linhas.Count; i++)
        {
            Cmd("caret.paraUp", new { extend = true });
        }

        Cmd("table.fromText", new { separator = "tab", columns = linhas[0].Length, autofit = ajusteAoConteudo ? "contents" : "window" });
        Cmd("table.selectTable");
        Cmd("table.borders", new { kind = "none" });
        if (comLinhas)
        {
            Cmd("table.borders", new { kind = "top", width = 0.5, color = Linha });
            Cmd("table.borders", new { kind = "bottom", width = 0.5, color = Linha });
        }

        Cmd("table.cellMargins", new { top = 4, bottom = 4, left = 0, right = 12 });
        Cmd("caret.docEnd");
        Paragrafo();
    }

    protected void Texto(string texto, string? fonte = null, double? tamanho = null, string? cor = null, bool negrito = false, double? espacamento = null)
    {
        if (fonte is not null)
        {
            Cmd("format.font", new { name = fonte });
        }

        if (tamanho is not null)
        {
            Cmd("format.size", new { size = tamanho });
        }

        if (cor is not null)
        {
            Cmd("format.color", new { color = cor });
        }

        if (negrito)
        {
            Cmd("format.bold", new { value = true });
        }

        if (espacamento is not null)
        {
            Cmd("format.spacing", new { points = espacamento });
        }

        // paragraphs: quebras de linha do texto viram parágrafos no mesmo estilo.
        Cmd("text.insert", new { text = texto, paragraphs = true });
        Cmd("format.clear");
    }

    protected void Paragrafo()
    {
        Cmd("text.newParagraph");
        Cmd("format.clear");
    }

    protected void Cmd(string comando, object? parametros = null)
        => Comandos.Add(parametros is null ? comando : new { command = comando, @params = parametros });

    /// <summary>
    /// Uma célula de tabela com o seu estilo (fonte, tamanho e cor opcionais).
    /// </summary>
    protected sealed record Celula(string Texto, string? Fonte = null, double? Tamanho = null, string? Cor = null);
}
