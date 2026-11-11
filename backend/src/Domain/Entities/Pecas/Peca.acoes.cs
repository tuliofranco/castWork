using Castwork.Domain.Common;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Pecas;

/// <summary>
/// Ações da <see cref="Peca"/>.
/// </summary>
public sealed partial class Peca
{
    /// <summary>
    /// Pela tela, a peça só nasce da publicação de uma peça do projeto (<see cref="Projetos.PecaDoProjeto"/>). O material
    /// é a <paramref name="liga"/> (ativa) ou, na pedra, o <paramref name="materialLivre"/>.
    /// </summary>
    public static Peca Criar(string nome, TipoPeca tipo, Liga? liga, string? materialLivre, Peso? peso, string? descricao)
    {
        var peca = new Peca { Ativo = true };
        peca.AtribuirDados(nome, tipo, MaterialDaPeca.Escolher(tipo, liga, materialLivre, atual: null), peso, descricao);
        return peca;
    }

    /// <summary>
    /// Mesma liga já gravada mantém a cópia do nome e do teor (ver <see cref="MaterialDaPeca.Escolher"/>).
    /// </summary>
    public void Atualizar(string nome, TipoPeca tipo, Liga? liga, string? materialLivre, Peso? peso, string? descricao)
        => AtribuirDados(nome, tipo, MaterialDaPeca.Escolher(tipo, liga, materialLivre, (Tipo, ObterMaterial())), peso, descricao);

    public void Desativar() => Ativo = false;

    public void Reativar() => Ativo = true;

    /// <summary>
    /// Guarda o desenho de uma ferramenta (o arquivo atual da peça do projeto, ao publicar). Um por ferramenta: guardar de
    /// novo substitui. Com as <paramref name="imagens"/> guardadas do arquivo (vista e conteúdo), quando já existem.
    /// </summary>
    public void GuardarDesenho(string ferramenta, byte[] conteudo, DateTimeOffset em, IEnumerable<(string Vista, byte[] Conteudo)>? imagens = null)
    {
        var desenho = DesenhoDaPeca.Criar(ferramenta, conteudo, imagens ?? [], em);
        _desenhos.RemoveAll(d => d.Ferramenta == desenho.Ferramenta);
        _desenhos.Add(desenho);
    }

    /// <summary>
    /// A peça da biblioteca criada ao publicar uma peça do projeto: leva a cópia da liga como está na peça do projeto.
    /// </summary>
    internal static Peca Publicada(string nome, TipoPeca tipo, MaterialDaPeca material, Peso? peso, string? descricao)
    {
        var peca = new Peca { Ativo = true };
        peca.AtribuirDados(nome, tipo, material, peso, descricao);
        return peca;
    }

    internal MaterialDaPeca ObterMaterial() => new(LigaId, Material, Teor);

    private void AtribuirDados(string nome, TipoPeca tipo, MaterialDaPeca material, Peso? peso, string? descricao)
    {
        Nome = Guard.Obrigatorio(nome, nameof(Nome), NomeMaxLength);
        Tipo = Enum.IsDefined(tipo) ? tipo : throw new ValorInvalidoException(nameof(Tipo), "não é um tipo de peça válido");
        (LigaId, Material, Teor) = material;
        Peso = peso;
        Descricao = Guard.Opcional(descricao, nameof(Descricao), DescricaoMaxLength);
    }
}
