using System.Text;

using Castwork.Application.Abstractions;

namespace Castwork.Infra.IA.Assistente.Conversa;

/// <summary>
/// Os arquivos do projeto anexados à fala, do jeito que a IA consegue usar: PDF vai como documento (a IA
/// lê); com servidor MCP, o arquivo vai para a pasta do turno e a IA recebe o caminho (o SolidCraft importa
/// o DXF do desenho 2D); sem MCP, texto pequeno vai no próprio bloco. O resto a IA só fica sabendo que veio.
/// </summary>
internal static class AnexosDaFala
{
    private const int TextoMaximo = 200_000;

    /// <summary>
    /// Grava os anexos em <c>anexos/</c> na pasta do turno (quando há) e monta os blocos da fala. Os PDFs ficam
    /// em <paramref name="documentos"/> pela chave do bloco: vão à IA só neste turno.
    /// </summary>
    public static async Task<List<BlocoGuardado>> PrepararAsync(
        IReadOnlyList<AnexoDoTurno> anexos,
        IReadOnlyDictionary<string, DefinicaoDeFerramenta> definicoes,
        string? pasta,
        Dictionary<Guid, byte[]> documentos,
        CancellationToken cancellationToken)
    {
        var blocos = new List<BlocoGuardado>();
        foreach (var anexo in anexos)
        {
            var extensao = (anexo.Extensao ?? definicoes.GetValueOrDefault(anexo.Ferramenta)?.ExtensaoDoArquivo ?? string.Empty).ToLowerInvariant();
            var nome = NomeSeguro(anexo.Nome, extensao);
            var origem = $"Anexado do projeto: {nome} ({anexo.Descricao}).";

            if (extensao == ".pdf")
            {
                var chave = Guid.CreateVersion7();
                documentos[chave] = anexo.Conteudo;
                blocos.Add(new BlocoGuardado("documento", origem, Id: chave.ToString()));
            }
            else if (pasta is not null)
            {
                var caminho = Path.Combine(pasta, "anexos", nome);
                Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
                await File.WriteAllBytesAsync(caminho, anexo.Conteudo, cancellationToken);
                blocos.Add(new BlocoGuardado("texto", $"{origem} O arquivo está em {caminho} (só neste turno: use agora o que precisar dele)."));
            }
            else if (anexo.Conteudo.Length <= TextoMaximo && EhTexto(anexo.Conteudo))
            {
                blocos.Add(new BlocoGuardado("texto", $"{origem} Conteúdo:\n{Encoding.UTF8.GetString(anexo.Conteudo)}"));
            }
            else
            {
                blocos.Add(new BlocoGuardado("texto", $"{origem} Este tipo de arquivo não pode ser lido nesta ferramenta."));
            }
        }

        return blocos;
    }

    /// <summary>
    /// Nome de arquivo sem acento, espaço nem caminho, com a extensão (a mesma regra do front).
    /// </summary>
    private static string NomeSeguro(string nome, string extensao)
    {
        var sem = nome.Normalize(NormalizationForm.FormD);
        var limpo = new StringBuilder();
        foreach (var c in sem.ToLowerInvariant())
        {
            if (char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            limpo.Append(char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' ? c : '-');
        }

        var basico = limpo.ToString().Trim('-', '.');
        if (extensao.Length > 0 && basico.EndsWith(extensao, StringComparison.Ordinal))
        {
            basico = basico[..^extensao.Length];
        }

        return (basico.Length > 0 ? basico : "anexo") + extensao;
    }

    private static bool EhTexto(byte[] conteudo) => !conteudo.Contains((byte)0);
}
