using Castwork.Application.Abstractions;

namespace Castwork.Infra.Documentos.WordCraft;

/// <summary>
/// Monta, comando a comando, o roteiro que o <c>wordcraft-cli run --script</c> executa. Os textos entram só
/// como parâmetro de <c>text.insert</c>, nunca como comando: nada que a IA escreva vira instrução.
/// Layout: marca no topo, número e título, o que será feito, especificações em tabela, prazo e investimento,
/// observações, aviso, aprovação e contatos no rodapé.
/// </summary>
internal sealed class RoteiroDoBriefing
{
    private const string Tinta = "2A2622";
    private const string Suave = "6B645C";
    private const string Linha = "E3DDD3";
    private const string Serifada = "Fraunces";
    private const string SemSerifa = "Instrument Sans";

    private readonly List<object> _comandos = [];
    private readonly string _cor;

    private RoteiroDoBriefing(string cor) => _cor = cor.TrimStart('#');

    public static List<object> Montar(DadosDocumentoBriefing dados, IReadOnlyList<(string Nome, byte[] Conteudo)> fontes)
    {
        var r = new RoteiroDoBriefing(dados.Marca.Cor);
        foreach (var (nome, conteudo) in fontes)
        {
            r.Cmd("font.add", new { data = Convert.ToBase64String(conteudo), name = nome });
        }

        r.Cmd("layout.size", new { name = "A4" });
        r.Cmd("layout.margins", new { top = 48, bottom = 56, left = 56, right = 56 });
        r.Cmd("styles.modify", new { style = "Normal", chr = new { font = SemSerifa, size = 10.5, color = Tinta }, para = new { spaceAfter = 4 } });

        r.Cabecalho(dados);
        r.Titulo(dados);

        r.Secao("O que será feito");
        r.Texto(dados.OQueSeraFeito);
        r.Paragrafo();

        r.Secao("Especificações");
        r.Tabela([.. dados.Especificacoes.Select(e => new[] { Rotulo(e.Rotulo), new Celula(e.Valor) })], comLinhas: true, ajusteAoConteudo: true);

        r.Secao("Prazo e investimento");
        r.Tabela(
            [
                [Rotulo("Entrega até", 9), Rotulo("Investimento", 9)],
                [new Celula(dados.Prazo ?? "A definir", Serifada, 16), new Celula(dados.Investimento ?? "A definir", Serifada, 16)],
            ],
            comLinhas: false);

        if (!string.IsNullOrWhiteSpace(dados.Observacoes))
        {
            r.Secao("Observações");
            r.Texto(dados.Observacoes);
            r.Paragrafo();
        }

        if (!string.IsNullOrWhiteSpace(dados.Aviso))
        {
            r.Texto(dados.Aviso, tamanho: 9, cor: Suave);
            r.Cmd("para.spacing", new { before = 10 });
            r.Paragrafo();
        }

        r.Aprovacao();
        r.Cmd("insert.footer", new { text = string.Join("  ·  ", dados.Marca.Contatos.DefaultIfEmpty(dados.Marca.Nome)) });
        return r._comandos;
    }

    private static Celula Rotulo(string texto, double? tamanho = null) => new(texto, Tamanho: tamanho, Cor: Suave);

    private void Cabecalho(DadosDocumentoBriefing dados)
    {
        if (dados.Marca.Logo is { Length: > 0 } logo)
        {
            Cmd("insert.picture", new { data = Convert.ToBase64String(logo), width = 40, alt = $"Logo {dados.Marca.Nome}" });
            Texto("   ");
        }

        Texto(dados.Marca.Nome, fonte: Serifada, tamanho: 18);
        Paragrafo();

        var linha = string.Join(" · ", new[] { dados.Marca.Assinatura, dados.Marca.Cidade }.Where(t => !string.IsNullOrWhiteSpace(t)));
        Texto(string.IsNullOrEmpty(linha) ? " " : linha, tamanho: 9, cor: Suave);
        Cmd("para.borders", new { kind = "bottom", width = 0.75, color = _cor });
        Cmd("para.spacing", new { after = 18 });
        Paragrafo();
        Cmd("para.borders", new { kind = "none" });
    }

    private void Titulo(DadosDocumentoBriefing dados)
    {
        Texto($"BRIEFING  Nº {dados.Numero:0000}", tamanho: 8.5, cor: _cor, negrito: true, espacamento: 1.6);
        Cmd("para.spacing", new { before = 4, after = 0 });
        Paragrafo();

        Texto(dados.Titulo, fonte: Serifada, tamanho: 24);
        Cmd("para.spacing", new { before = 6, after = 2 });
        Paragrafo();

        var para = dados.Cliente is null ? "Projeto autoral" : $"Para {dados.Cliente}";
        Texto($"{para}  ·  emitido em {dados.Data:dd/MM/yyyy}", tamanho: 9.5, cor: Suave);
        Cmd("para.spacing", new { after = 6 });
        Paragrafo();
    }

    private void Aprovacao()
    {
        Secao("Aprovação");
        Texto("☐  Aprovado        ☐  Aprovado com ajustes        ☐  Não aprovado");
        Cmd("para.spacing", new { after = 22 });
        Paragrafo();
        Tabela(
            [
                [new Celula("______________________________"), new Celula("______________________________"), new Celula("____/____/____")],
                [Rotulo("Nome", 9), Rotulo("Assinatura", 9), Rotulo("Data", 9)],
            ],
            comLinhas: false);
    }

    private void Secao(string titulo)
    {
        Texto(titulo.ToUpperInvariant(), tamanho: 8, cor: _cor, negrito: true, espacamento: 1.6);
        Cmd("para.spacing", new { before = 14, after = 6 });
        Cmd("para.keepNext", new { value = true });
        Paragrafo();
    }

    /// <summary>
    /// Escreve as linhas separadas por tabulação e converte o trecho em tabela: o WordCraft não navega
    /// entre células por comando, mas converte texto em tabela de forma estável. Com <c>ajusteAoConteudo</c>
    /// as colunas seguem o texto (especificações); sem ele, dividem a largura da página.
    /// </summary>
    private void Tabela(List<Celula[]> linhas, bool comLinhas, bool ajusteAoConteudo = false)
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

    private void Texto(string texto, string? fonte = null, double? tamanho = null, string? cor = null, bool negrito = false, double? espacamento = null)
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

    private void Paragrafo()
    {
        Cmd("text.newParagraph");
        Cmd("format.clear");
    }

    private void Cmd(string comando, object? parametros = null)
        => _comandos.Add(parametros is null ? comando : new { command = comando, @params = parametros });

    /// <summary>
    /// Uma célula de tabela com o seu estilo (fonte, tamanho e cor opcionais).
    /// </summary>
    private sealed record Celula(string Texto, string? Fonte = null, double? Tamanho = null, string? Cor = null);
}
