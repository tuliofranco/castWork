using Castwork.Application.Abstractions;
using Castwork.Application.Assistente;
using Castwork.Application.Common.Exceptions;
using Castwork.Application.Projetos.Commands;
using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;

namespace Castwork.Application.Ferramentas.Modelo3d.Commands;

/// <summary>
/// Base dos commands da conferência de peso e materiais (<c>/projetos/{projetoId}/versoes/{versaoId}/modelo-3d</c>).
/// <see cref="ArquivoId"/>: o arquivo 3D que a conferência mostrou; se o modelo mudou depois (a IA ou o joalheiro
/// salvou), a alteração é recusada, porque os corpos podem não ser mais os mesmos.
/// </summary>
public abstract class ComandoDoModelo : ComandoDeVersao
{
    public long ArquivoId { get; set; }
}

/// <summary>
/// O arquivo que a conferência gravou (<see cref="ArquivoId"/>) e o de antes (<see cref="ArquivoAnteriorId"/>), que o
/// Desfazer restaura.
/// </summary>
public sealed class ModeloAlteradoDto
{
    public long ArquivoId { get; init; }

    public long ArquivoAnteriorId { get; init; }
}

/// <summary>
/// O caminho comum das alterações da conferência: versão em desenvolvimento, sem a IA trabalhando no modelo, no arquivo
/// atual (o mesmo que a tela mostrou) e medido; a alteração é aplicada pelo <see cref="IEditorDoModelo"/>, sem IA, e o
/// resultado vira uma nova versão do arquivo (<see cref="OrigemArquivo.Conferencia"/>), desfazível pelo histórico. A
/// medição guardada (etapa 6) é pelo id do arquivo: o arquivo novo é medido de novo.
/// </summary>
internal static class AlteracaoNaConferencia
{
    public static async Task<ModeloAlteradoDto> AplicarAsync(
        IUnitOfWork unitOfWork,
        IMedidorDoModelo medidor,
        IEditorDoModelo editor,
        TimeProvider relogio,
        ComandoDoModelo request,
        Func<ModeloAtual, AlteracaoDoModelo> montar,
        CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        projeto.GarantirVersaoEmDesenvolvimento(request.VersaoId);

        var agora = relogio.GetUtcNow();
        var conversa = await ConversaDaVersao.ObterAsync(unitOfWork, request.ProjetoId, request.VersaoId, Modelo3dNoAssistente.Codigo, cancellationToken)
            ?? throw new RegraDeNegocioException("A versão não tem modelo 3D.");
        if (conversa.AguardandoIa(agora))
        {
            throw new RegraDeNegocioException("A IA está editando o modelo. Espere ela terminar para alterar os materiais.");
        }

        var modelo = await PesoDoModelo.MedirArquivoAtualAsync(unitOfWork, medidor, request.VersaoId, cancellationToken)
            ?? throw new RegraDeNegocioException("A versão não tem modelo 3D.");
        if (modelo.ArquivoId != request.ArquivoId)
        {
            throw new RegraDeNegocioException("O modelo 3D mudou desde que a conferência foi aberta. Confira de novo.");
        }

        if (modelo.Medicao is null)
        {
            throw new ServicoIndisponivelException("Não foi possível medir o modelo 3D. Tente de novo em instantes.");
        }

        var alteracao = montar(modelo);
        var conteudo = await unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => a.Id == modelo.ArquivoId)
            .Select(a => a.Conteudo)
            .SingleAsync(cancellationToken);
        var alterado = await editor.AlterarAsync(conteudo, modelo.Materiais, alteracao, cancellationToken);

        var arquivo = ArquivoFerramenta.Criar(conversa.Id, alterado, OrigemArquivo.Conferencia, agora);
        unitOfWork.GetRepository<ArquivoFerramenta>().Adicionar(arquivo);
        await unitOfWork.SalvarAsync(cancellationToken);
        return new ModeloAlteradoDto { ArquivoId = arquivo.Id, ArquivoAnteriorId = modelo.ArquivoId };
    }

    /// <summary>
    /// Os corpos pedidos, como estão no modelo medido; um nome que não está no modelo é recusado.
    /// </summary>
    public static IReadOnlyList<CorpoMedido> Corpos(MedicaoDoModelo medicao, IEnumerable<string> nomes)
        => [.. nomes.Select(nome => medicao.Corpos.FirstOrDefault(c => c.Nome == nome)
            ?? throw new RegraDeNegocioException($"O corpo \"{nome}\" não está no modelo 3D. Confira de novo."))];
}
