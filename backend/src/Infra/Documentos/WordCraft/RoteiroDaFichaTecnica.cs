using Castwork.Application.Abstractions;

namespace Castwork.Infra.Documentos.WordCraft;

/// <summary>
/// O roteiro do PDF da ficha técnica da peça, no visual de <see cref="RoteiroWordCraft"/>.
/// Layout: marca no topo, "Ficha técnica" e o nome da peça, projeto e data, especificações em tabela, as imagens do
/// desenho (só quando vêm), descrição, valor no projeto (só quando vem), resumo do briefing e contatos no rodapé.
/// </summary>
internal sealed class RoteiroDaFichaTecnica : RoteiroWordCraft
{
    private RoteiroDaFichaTecnica(string cor)
        : base(cor)
    {
    }

    public static List<object> Montar(DadosFichaTecnica dados)
    {
        var r = new RoteiroDaFichaTecnica(dados.Marca.Cor);
        r.Inicio();
        r.Cabecalho(dados.Marca);
        r.Titulo(dados);

        r.Secao("Especificações");
        r.Tabela([.. dados.Especificacoes.Select(e => new[] { Rotulo(e.Rotulo), new Celula(e.Valor) })], comLinhas: true, ajusteAoConteudo: true);

        if (dados.Imagens is { Count: > 0 } imagens)
        {
            r.Secao("Desenho");
            foreach (var imagem in imagens)
            {
                // A largura útil da página A4 com as margens do roteiro (595 - 56 - 56 pt); a altura segue a imagem.
                r.Cmd("insert.picture", new { data = Convert.ToBase64String(imagem.Png), width = 440, alt = imagem.Legenda });
                r.Paragrafo();
                r.Texto(imagem.Legenda, tamanho: 9, cor: Suave);
                r.Cmd("para.spacing", new { after = 10 });
                r.Paragrafo();
            }
        }

        if (!string.IsNullOrWhiteSpace(dados.Descricao))
        {
            r.Secao("Descrição");
            r.Texto(dados.Descricao);
            r.Paragrafo();
        }

        if (dados.Valor is not null)
        {
            r.Secao("Valor no projeto");
            r.Texto(dados.Valor, fonte: Serifada, tamanho: 16);
            r.Paragrafo();
        }

        if (!string.IsNullOrWhiteSpace(dados.Briefing))
        {
            r.Secao("Briefing do projeto");
            r.Texto(dados.Briefing);
            r.Paragrafo();
        }

        r.Rodape(dados.Marca);
        return r.Comandos;
    }

    private void Titulo(DadosFichaTecnica dados)
    {
        Texto("FICHA TÉCNICA", tamanho: 8.5, cor: Cor, negrito: true, espacamento: 1.6);
        Cmd("para.spacing", new { before = 4, after = 0 });
        Paragrafo();

        Texto(dados.Nome, fonte: Serifada, tamanho: 24);
        Cmd("para.spacing", new { before = 6, after = 2 });
        Paragrafo();

        var projeto = dados.Cliente is null ? $"Projeto {dados.Projeto}" : $"Projeto {dados.Projeto}, para {dados.Cliente}";
        Texto($"{projeto}  ·  emitida em {dados.Data:dd/MM/yyyy}", tamanho: 9.5, cor: Suave);
        Cmd("para.spacing", new { after = 6 });
        Paragrafo();
    }
}
