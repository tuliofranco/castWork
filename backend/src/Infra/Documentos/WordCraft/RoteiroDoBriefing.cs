using Castwork.Application.Abstractions;

namespace Castwork.Infra.Documentos.WordCraft;

/// <summary>
/// O roteiro do PDF do briefing, no visual de <see cref="RoteiroWordCraft"/>.
/// Layout: marca no topo, número e título, o que será feito, especificações em tabela, prazo e investimento,
/// observações, aviso, aprovação e contatos no rodapé.
/// </summary>
internal sealed class RoteiroDoBriefing : RoteiroWordCraft
{
    private RoteiroDoBriefing(string cor)
        : base(cor)
    {
    }

    public static List<object> Montar(DadosDocumentoBriefing dados)
    {
        var r = new RoteiroDoBriefing(dados.Marca.Cor);
        r.Inicio();
        r.Cabecalho(dados.Marca);
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
        r.Rodape(dados.Marca);
        return r.Comandos;
    }

    private void Titulo(DadosDocumentoBriefing dados)
    {
        Texto($"BRIEFING  Nº {dados.Numero:0000}", tamanho: 8.5, cor: Cor, negrito: true, espacamento: 1.6);
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
}
