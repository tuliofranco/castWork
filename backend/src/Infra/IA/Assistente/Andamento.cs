using Anthropic.Models.Beta.Messages;

using Castwork.Application.Abstractions;

namespace Castwork.Infra.IA.Assistente;

/// <summary>
/// O andamento de um turno: repassa ao painel o pensamento, o texto e os passos à medida que acontecem, e
/// guarda o que fica na resposta final (resumo do pensamento, passos e tokens consumidos).
/// </summary>
internal sealed class Andamento(Action<EventoDoTurno> observar, string modelo)
{
    private const string Separador = "\n\n";

    private readonly List<string> _passos = [];
    private readonly List<string> _pensamentos = [];
    private int _ultimoPasso;
    private bool _textoNestaChamada;
    private bool _pensamentoNestaChamada;
    private bool _jaEscreveu;
    private bool _jaPensou;
    private int _chamadas;
    private long _entrada;
    private long _saida;
    private long _cacheLeitura;
    private long _cacheEscrita;

    public string? Pensamento => _pensamentos.Count == 0 ? null : string.Join(Separador, _pensamentos);

    public IReadOnlyList<string> Passos => _passos;

    public ConsumoDoTurno Consumo => new(modelo, _chamadas, (int)_entrada, (int)_saida, (int)_cacheLeitura, (int)_cacheEscrita);

    /// <summary>
    /// Cada chamada à API recomeça o texto e o pensamento: na tela, o trecho novo vem separado do anterior.
    /// </summary>
    public void NovaChamada()
    {
        _textoNestaChamada = false;
        _pensamentoNestaChamada = false;
    }

    public void Pensando(string trecho)
    {
        if (!_pensamentoNestaChamada && _jaPensou)
        {
            observar(new EventoDoTurno("pensamento", Separador));
        }

        _pensamentoNestaChamada = _jaPensou = true;
        observar(new EventoDoTurno("pensamento", trecho));
    }

    public void Escrevendo(string trecho)
    {
        if (!_textoNestaChamada && _jaEscreveu)
        {
            observar(new EventoDoTurno("texto", Separador));
        }

        _textoNestaChamada = _jaEscreveu = true;
        observar(new EventoDoTurno("texto", trecho));
    }

    /// <summary>
    /// O resumo do pensamento de uma chamada, já completo, para guardar com a resposta.
    /// </summary>
    public void Pensou(IEnumerable<string?> resumos)
        => _pensamentos.AddRange(resumos.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r!.Trim()));

    /// <summary>
    /// Começa um passo (uma ferramenta do MCP). Sem descrição, o passo não aparece; devolve 0.
    /// </summary>
    public int Comecar(string? descricao)
    {
        if (descricao is null)
        {
            return 0;
        }

        // Passos iguais em sequência viram um só no resumo ("Desenhando arcos" várias vezes seguidas).
        if (_passos.Count == 0 || _passos[^1] != descricao)
        {
            _passos.Add(descricao);
        }

        observar(new EventoDoTurno("passo", descricao, ++_ultimoPasso, "andamento"));
        return _ultimoPasso;
    }

    public void Terminar(int passo, bool erro, ImagemDaConversa? vista)
    {
        if (passo == 0)
        {
            return;
        }

        var imagem = vista is null ? null : Convert.ToBase64String(vista.Conteudo);
        observar(new EventoDoTurno("passo", null, passo, erro ? "erro" : "feito", imagem));
    }

    public void Somar(BetaUsage uso)
    {
        _chamadas++;
        _entrada += uso.InputTokens;
        _saida += uso.OutputTokens;
        _cacheLeitura += uso.CacheReadInputTokens ?? 0;
        _cacheEscrita += uso.CacheCreationInputTokens ?? 0;
    }
}
