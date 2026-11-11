using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Assistente;

using FluentValidation;

using MediatR;

namespace Castwork.Application.Assistente.Commands;

/// <summary>
/// Grava o arquivo editado pelo joalheiro na tela (o DXF salvo no CADCraft). Vira a versão atual: a IA
/// passa a trabalhar em cima dele. Numa peça do projeto publicada, o desenho novo faz dela outra peça (volta a rascunho).
/// Retorna o Id da nova versão do arquivo.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class SalvarArquivoFerramentaCommand : ComandoDeFerramenta, IRequest<long>
{
    /// <summary>
    /// O arquivo em base64.
    /// </summary>
    public string Conteudo { get; set; } = string.Empty;
}

public sealed class SalvarArquivoFerramentaCommandHandler(IUnitOfWork unitOfWork, TimeProvider relogio)
    : IRequestHandler<SalvarArquivoFerramentaCommand, long>
{
    public async Task<long> Handle(SalvarArquivoFerramentaCommand request, CancellationToken cancellationToken)
    {
        await ConversaDoEscopo.GarantirEscopoAsync(unitOfWork, request, cancellationToken);
        var agora = relogio.GetUtcNow();
        var conversa = await ConversaDoEscopo.ObterOuCriarAsync(unitOfWork, request, request.Ferramenta, agora, cancellationToken);

        var arquivo = ArquivoFerramenta.Criar(conversa.Id, Convert.FromBase64String(request.Conteudo), OrigemArquivo.Joalheiro, agora);
        unitOfWork.GetRepository<ArquivoFerramenta>().Adicionar(arquivo);
        await unitOfWork.SalvarAsync(cancellationToken);
        return arquivo.Id;
    }
}

public sealed class SalvarArquivoFerramentaCommandValidator : ValidatorBase<SalvarArquivoFerramentaCommand>
{
    public SalvarArquivoFerramentaCommandValidator(IUnitOfWork unitOfWork, CatalogoDeFerramentas catalogo)
        : base(unitOfWork)
    {
        this.ValidarFerramenta(UnitOfWork, catalogo);
        RuleFor(c => c.Conteudo)
            .Must(c => !string.IsNullOrEmpty(c) && Convert.TryFromBase64String(c, new byte[c.Length], out _))
            .WithMessage("Arquivo inválido.");
    }
}
