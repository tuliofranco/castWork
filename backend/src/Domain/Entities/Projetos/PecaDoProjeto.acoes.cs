using Castwork.Domain.Common;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Projetos;

/// <summary>
/// Ações da <see cref="PecaDoProjeto"/>. Internas: só o agregado <see cref="Projeto"/> as chama.
/// </summary>
public sealed partial class PecaDoProjeto
{
    /// <summary>
    /// Peça criada no projeto: rascunho, sem vínculo com a biblioteca e sem valor. O material é a <paramref name="liga"/>
    /// (ativa) ou, na pedra, o <paramref name="materialLivre"/>.
    /// </summary>
    internal static PecaDoProjeto Nova(string nome, TipoPeca tipo, Liga? liga, string? materialLivre, Peso? peso, string? descricao)
    {
        var peca = new PecaDoProjeto { Ativo = true, Status = StatusPecaDoProjeto.Rascunho };
        peca.AtribuirDados(nome, tipo, MaterialDaPeca.Escolher(tipo, liga, materialLivre, atual: null), peso, descricao);
        return peca;
    }

    /// <summary>
    /// Cópia de uma peça da biblioteca: publicada e vinculada a ela, com a mesma liga (Id e cópia do nome e do teor). O
    /// valor começa vazio: cada projeto define o seu.
    /// </summary>
    internal static PecaDoProjeto ImportadaDe(Peca peca)
    {
        ArgumentNullException.ThrowIfNull(peca);
        var copia = new PecaDoProjeto
        {
            Ativo = true,
            Status = StatusPecaDoProjeto.Publicada,
            PecaId = Guard.IdValido(peca.Id, nameof(PecaId)),
        };
        copia.AtribuirDados(peca.Nome, peca.Tipo, peca.ObterMaterial(), peca.Peso, peca.Descricao);
        return copia;
    }

    /// <summary>
    /// Altera os dados. Se algum mudou numa peça publicada, ela vira <b>outra peça</b>: volta a rascunho e perde o vínculo
    /// (a peça da biblioteca não muda). Trocar a liga conta como alterar; a mesma liga mantém a cópia do nome e do teor
    /// (ver <see cref="MaterialDaPeca.Escolher"/>). Ativar ou desativar não muda a identidade.
    /// </summary>
    /// <returns>Se a peça deixou de ser a publicada (voltou a rascunho).</returns>
    internal bool AlterarDados(string nome, TipoPeca tipo, Liga? liga, string? materialLivre, Peso? peso, string? descricao, bool ativo)
    {
        var anterior = (Nome, Tipo, ObterMaterial(), Peso, Descricao);
        AtribuirDados(nome, tipo, MaterialDaPeca.Escolher(tipo, liga, materialLivre, (Tipo, ObterMaterial())), peso, descricao);
        Ativo = ativo;

        return anterior != (Nome, Tipo, ObterMaterial(), Peso, Descricao) && VirarOutraPeca();
    }

    /// <summary>
    /// O desenho da peça mudou (o joalheiro marcou outro arquivo como final no desenho 2D ou no modelo 3D dela; salvar um
    /// arquivo novo sem marcá-lo não conta). Como alterar os dados: a peça
    /// publicada vira <b>outra peça</b>, volta a rascunho e perde o vínculo (a peça da biblioteca e o desenho dela não
    /// mudam). Em rascunho, nada muda.
    /// </summary>
    /// <returns>Se a peça deixou de ser a publicada (voltou a rascunho).</returns>
    internal bool AlterarDesenho() => VirarOutraPeca();

    /// <summary>
    /// Custo de uma unidade neste projeto (nulo tira o valor). Não muda a identidade, o status nem o vínculo.
    /// </summary>
    internal void DefinirValor(Dinheiro? valor) => Valor = valor;

    /// <summary>
    /// Cria a peça na biblioteca com os dados desta (sem valor, com a mesma liga e a mesma cópia) e passa a ser a publicada. O <see cref="PecaId"/> é
    /// preenchido ao salvar, com o Id da peça nova. O desenho (2D e 3D) vai junto: quem publica guarda na peça nova os
    /// arquivos finais (<see cref="Peca.GuardarDesenho"/>).
    /// </summary>
    internal Peca Publicar()
    {
        if (Status != StatusPecaDoProjeto.Rascunho)
        {
            throw new RegraDeNegocioException($"A peça {Nome} já está publicada na biblioteca.");
        }

        if (!Ativo)
        {
            throw new RegraDeNegocioException($"A peça {Nome} está desativada. Reative-a para publicar.");
        }

        var peca = Peca.Publicada(Nome, Tipo, ObterMaterial(), Peso, Descricao);
        PecaPublicada = peca;
        Status = StatusPecaDoProjeto.Publicada;
        return peca;
    }

    /// <summary>
    /// Publicada com algo diferente (dados ou desenho) é outra peça: volta a rascunho e perde o vínculo.
    /// </summary>
    private bool VirarOutraPeca()
    {
        if (Status != StatusPecaDoProjeto.Publicada)
        {
            return false;
        }

        Status = StatusPecaDoProjeto.Rascunho;
        PecaId = null;
        return true;
    }

    /// <summary>
    /// Mesmas regras da peça da biblioteca: nome obrigatório, tipo conhecido e os limites de tamanho.
    /// </summary>
    private void AtribuirDados(string nome, TipoPeca tipo, MaterialDaPeca material, Peso? peso, string? descricao)
    {
        Nome = Guard.Obrigatorio(nome, nameof(Nome), Peca.NomeMaxLength);
        Tipo = Enum.IsDefined(tipo) ? tipo : throw new ValorInvalidoException(nameof(Tipo), "não é um tipo de peça válido");
        (LigaId, Material, Teor) = material;
        Peso = peso;
        Descricao = Guard.Opcional(descricao, nameof(Descricao), Peca.DescricaoMaxLength);
    }

    private MaterialDaPeca ObterMaterial() => new(LigaId, Material, Teor);
}
