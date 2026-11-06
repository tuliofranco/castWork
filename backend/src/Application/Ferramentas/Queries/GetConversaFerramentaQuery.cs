using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Briefings;
using Castwork.Domain.Entities.Ferramentas;

using MediatR;

namespace Castwork.Application.Ferramentas.Queries;

/// <summary>
/// A conversa da ferramenta na versão; vazia (sem mensagens nem arquivo) se ainda não começou.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetConversaFerramentaQuery : IRequest<ConversaFerramentaDto>
{
    public long ProjetoId { get; set; }

    public long VersaoId { get; set; }

    public string Ferramenta { get; set; } = string.Empty;
}

public sealed class GetConversaFerramentaQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetConversaFerramentaQuery, ConversaFerramentaDto>
{
    public async Task<ConversaFerramentaDto> Handle(GetConversaFerramentaQuery request, CancellationToken cancellationToken)
    {
        await ConversaDaVersao.GarantirVersaoAsync(unitOfWork, request.ProjetoId, request.VersaoId, cancellationToken);
        var conversa = await ConversaDaVersao.ObterAsync(unitOfWork, request.VersaoId, request.Ferramenta, cancellationToken);
        if (conversa is null)
        {
            return new ConversaFerramentaDto([], null);
        }

        var arquivoId = await unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => a.ConversaId == conversa.Id)
            .OrderByDescending(a => a.Id)
            .Select(a => a.Conteudo.Length == 0 ? null : (long?)a.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return Mapear(conversa, arquivoId);
    }

    internal static ConversaFerramentaDto Mapear(ConversaFerramenta conversa, long? arquivoId) => new(
        conversa.Mensagens
            .Where(m => m.Papel != PapelMensagem.Ferramenta && (m.Texto is not null || m.Imagens.Count > 0))
            .OrderBy(m => m.Ordem)
            .Select(m => new MensagemFerramentaDto(
                m.Ordem,
                m.Papel,
                m.Texto,
                m.Imagens,
                m.Acao is null ? null : new AcaoDaIaDto(m.Acao.Descricao, m.AcaoDesfeita),
                m.CriadaEm))
            .ToList(),
        arquivoId);
}
