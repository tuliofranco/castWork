using System.Text.Json;

using Castwork.Infra.IA.Assistente.Mcp;

namespace Castwork.Infra.IA.Assistente;

/// <summary>
/// Garante no arquivo recém-aberto o que a ferramenta pede (<see cref="DefinicaoDeFerramenta.PrepararArquivo"/>): lê o
/// arquivo, monta só o que falta (ou mudou) e aplica numa chamada. Repetir não muda nada. Não é alteração da IA: o arquivo
/// só é gravado se a IA também mexer nele.
/// </summary>
internal static class PreparoDoArquivo
{
    /// <summary>
    /// Nulo quando deu certo (ou não havia o que fazer); senão, o erro do servidor, para o log. A IA modela do mesmo jeito.
    /// </summary>
    public static async Task<string?> AplicarAsync(ClienteMcp mcp, DefinicaoDeFerramenta definicao, string? preparo, CancellationToken cancellationToken)
    {
        if (preparo is null || definicao.PrepararArquivo is not { } preparar)
        {
            return null;
        }

        var leitura = await ChamarAsync(mcp, preparar.Leitura, cancellationToken);
        if (leitura.Erro)
        {
            return leitura.Texto;
        }

        if (preparar.Ajustar(leitura.Texto, preparo) is not { } ajuste)
        {
            return null;
        }

        var resultado = await ChamarAsync(mcp, ajuste, cancellationToken);
        return resultado.Erro ? resultado.Texto : null;
    }

    private static Task<ResultadoMcp> ChamarAsync(ClienteMcp mcp, (string Nome, string Argumentos) chamada, CancellationToken cancellationToken)
        => mcp.ChamarFerramentaAsync(chamada.Nome, JsonDocument.Parse(chamada.Argumentos).RootElement, cancellationToken);
}
