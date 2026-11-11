using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;

using FluentValidation;

using MediatR;

namespace Castwork.Application.Assistente.Commands;

/// <summary>
/// Marca um arquivo do histórico como o final da peça do projeto naquela ferramenta: é ele que vale para o peso, a montagem,
/// a publicação, o visualizador e a ficha técnica. A ferramenta pode ajustar o arquivo antes (<see cref="IArquivoFinalDaFerramenta"/>:
/// no modelo 3D, os corpos com o nome da peça), e o ajustado vira uma nova versão do arquivo, que é a marcada. Trocar o
/// final é alterar o desenho: a peça publicada vira outra peça. Marcar de novo o mesmo arquivo não muda nada.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class MarcarArquivoFinalFerramentaCommand : ComandoDeArquivo, IRequest<ArquivoFinalDto>;

/// <summary>
/// O arquivo que ficou como final (o ajustado, quando <see cref="Ajustado"/>: a tela recarrega o desenho) e se a peça
/// publicada virou outra peça.
/// </summary>
public sealed record ArquivoFinalDto(long ArquivoId, bool Ajustado, bool VirouOutraPeca);

public sealed class MarcarArquivoFinalFerramentaCommandHandler(IUnitOfWork unitOfWork, CatalogoDeFerramentas catalogo, TimeProvider relogio)
    : IRequestHandler<MarcarArquivoFinalFerramentaCommand, ArquivoFinalDto>
{
    public async Task<ArquivoFinalDto> Handle(MarcarArquivoFinalFerramentaCommand request, CancellationToken cancellationToken)
    {
        var arquivo = await ConversaDoEscopo.ObterArquivoAsync(unitOfWork, request, cancellationToken);
        var conversa = (await ConversaDoEscopo.ObterAsync(unitOfWork, request, request.Ferramenta, cancellationToken))!;
        var agora = relogio.GetUtcNow();
        if (conversa.AguardandoIa(agora))
        {
            throw new RegraDeNegocioException("A IA está trabalhando neste desenho. Espere ela terminar para marcar o arquivo final.");
        }

        if (conversa.ArquivoFinalId == arquivo.Id)
        {
            return new ArquivoFinalDto(arquivo.Id, Ajustado: false, VirouOutraPeca: false);
        }

        var final = arquivo;
        if (!arquivo.EmBranco && !arquivo.Arquivado && catalogo.Obter(request.Ferramenta) is IArquivoFinalDaFerramenta ajuste
            && await ajuste.AjustarAsync(arquivo.Id, arquivo.Conteudo, await NomeDaPecaAsync(request, cancellationToken), cancellationToken) is { } ajustado)
        {
            final = ArquivoFerramenta.Criar(conversa.Id, ajustado, OrigemArquivo.NomesDaPeca, agora);
            final.Renomear(arquivo.Nome);
            unitOfWork.GetRepository<ArquivoFerramenta>().Adicionar(final);
            await unitOfWork.SalvarAsync(cancellationToken);
        }

        conversa.MarcarArquivoFinal(final);
        var projetoId = request.ProjetoId;
        var projeto = await unitOfWork.GetRepository<Projeto>().FindBy(p => p.Id == projetoId).Include(p => p.Pecas).SingleAsync(cancellationToken);
        var virouOutraPeca = projeto.AlterarDesenhoDaPeca(request.PecaDoProjetoId);
        await unitOfWork.SalvarAsync(cancellationToken);
        return new ArquivoFinalDto(final.Id, final != arquivo, virouOutraPeca);
    }

    private Task<string> NomeDaPecaAsync(MarcarArquivoFinalFerramentaCommand request, CancellationToken cancellationToken)
    {
        var (projetoId, pecaId) = (request.ProjetoId, request.PecaDoProjetoId);
        return unitOfWork.GetRepository<Projeto>()
            .FindBy(p => p.Id == projetoId)
            .SelectMany(p => p.Pecas)
            .Where(p => p.Id == pecaId)
            .Select(p => p.Nome)
            .SingleAsync(cancellationToken);
    }
}

public sealed class MarcarArquivoFinalFerramentaCommandValidator : ValidatorBase<MarcarArquivoFinalFerramentaCommand>
{
    public MarcarArquivoFinalFerramentaCommandValidator(IUnitOfWork unitOfWork, CatalogoDeFerramentas catalogo)
        : base(unitOfWork)
    {
        this.ValidarFerramenta(UnitOfWork, catalogo);
        RuleFor(c => c.PecaDoProjetoId).GreaterThan(0).WithMessage("Só o desenho de uma peça do projeto tem arquivo final.");
    }
}
