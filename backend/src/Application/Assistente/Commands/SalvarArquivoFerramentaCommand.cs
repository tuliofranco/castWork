using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Assistente;

using FluentValidation;

using MediatR;

namespace Castwork.Application.Assistente.Commands;

/// <summary>
/// Grava o arquivo editado pelo joalheiro na tela (o DXF salvo no CADCraft). Vira a versão atual: a IA
/// passa a trabalhar em cima dele. Retorna o Id da nova versão do arquivo.
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
        await ConversaDaVersao.GarantirVersaoAsync(unitOfWork, request.ProjetoId, request.VersaoId, cancellationToken);
        var agora = relogio.GetUtcNow();
        var conversa = await ConversaDaVersao.ObterOuCriarAsync(unitOfWork, request.ProjetoId, request.VersaoId, request.Ferramenta, agora, cancellationToken);

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
