using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Assistente;

using FluentValidation;

using MediatR;

namespace Castwork.Application.Assistente.Commands;

/// <summary>
/// Dá um nome ao arquivo (vazio volta ao nome padrão da ferramenta).
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class RenomearArquivoFerramentaCommand : ComandoDeArquivo, IRequest
{
    public string? Nome { get; set; }
}

/// <summary>
/// Arquiva o arquivo: some da pasta e do histórico. Se era o atual, o anterior não arquivado passa a ser. O arquivo final
/// da peça do projeto não arquiva (marque outro antes).
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class ArquivarArquivoFerramentaCommand : ComandoDeArquivo, IRequest;

/// <summary>
/// Volta o arquivo para a pasta (e para atual, se for o mais recente).
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class DesarquivarArquivoFerramentaCommand : ComandoDeArquivo, IRequest;

/// <summary>
/// Apaga de vez um arquivo arquivado.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class ApagarArquivoFerramentaCommand : ComandoDeArquivo, IRequest;

public sealed class GerenciarArquivoFerramentaCommandHandler(IUnitOfWork unitOfWork, TimeProvider relogio)
    : IRequestHandler<RenomearArquivoFerramentaCommand>,
      IRequestHandler<ArquivarArquivoFerramentaCommand>,
      IRequestHandler<DesarquivarArquivoFerramentaCommand>,
      IRequestHandler<ApagarArquivoFerramentaCommand>
{
    public async Task Handle(RenomearArquivoFerramentaCommand request, CancellationToken cancellationToken)
    {
        var arquivo = await ConversaDoEscopo.ObterArquivoAsync(unitOfWork, request, cancellationToken);
        arquivo.Renomear(request.Nome);
        await unitOfWork.SalvarAsync(cancellationToken);
    }

    public async Task Handle(ArquivarArquivoFerramentaCommand request, CancellationToken cancellationToken)
    {
        var arquivo = await ConversaDoEscopo.ObterArquivoAsync(unitOfWork, request, cancellationToken);
        var conversa = await ConversaDoEscopo.ObterAsync(unitOfWork, request, request.Ferramenta, cancellationToken);
        conversa!.GarantirQuePodeArquivar(arquivo);
        arquivo.Arquivar(relogio.GetUtcNow());
        await unitOfWork.SalvarAsync(cancellationToken);
    }

    public async Task Handle(DesarquivarArquivoFerramentaCommand request, CancellationToken cancellationToken)
    {
        var arquivo = await ConversaDoEscopo.ObterArquivoAsync(unitOfWork, request, cancellationToken);
        arquivo.Desarquivar();
        await unitOfWork.SalvarAsync(cancellationToken);
    }

    public async Task Handle(ApagarArquivoFerramentaCommand request, CancellationToken cancellationToken)
    {
        var arquivo = await ConversaDoEscopo.ObterArquivoAsync(unitOfWork, request, cancellationToken);
        arquivo.GarantirQuePodeApagar();
        unitOfWork.GetRepository<ArquivoFerramenta>().Remover(arquivo);
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}

public sealed class RenomearArquivoFerramentaCommandValidator : ValidatorBase<RenomearArquivoFerramentaCommand>
{
    public RenomearArquivoFerramentaCommandValidator(IUnitOfWork unitOfWork, CatalogoDeFerramentas catalogo)
        : base(unitOfWork)
    {
        this.ValidarFerramenta(UnitOfWork, catalogo);
        RuleFor(c => c.Nome).MaximumLength(ArquivoFerramenta.NomeMaxLength).WithName("Nome");
    }
}

public sealed class ArquivarArquivoFerramentaCommandValidator : ValidatorBase<ArquivarArquivoFerramentaCommand>
{
    public ArquivarArquivoFerramentaCommandValidator(IUnitOfWork unitOfWork, CatalogoDeFerramentas catalogo)
        : base(unitOfWork) => this.ValidarFerramenta(UnitOfWork, catalogo);
}

public sealed class DesarquivarArquivoFerramentaCommandValidator : ValidatorBase<DesarquivarArquivoFerramentaCommand>
{
    public DesarquivarArquivoFerramentaCommandValidator(IUnitOfWork unitOfWork, CatalogoDeFerramentas catalogo)
        : base(unitOfWork) => this.ValidarFerramenta(UnitOfWork, catalogo);
}

public sealed class ApagarArquivoFerramentaCommandValidator : ValidatorBase<ApagarArquivoFerramentaCommand>
{
    public ApagarArquivoFerramentaCommandValidator(IUnitOfWork unitOfWork, CatalogoDeFerramentas catalogo)
        : base(unitOfWork) => this.ValidarFerramenta(UnitOfWork, catalogo);
}
