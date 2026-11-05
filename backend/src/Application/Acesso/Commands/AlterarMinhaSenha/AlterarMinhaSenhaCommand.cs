using Castwork.Application.Abstractions;
using Castwork.Application.Acesso.Common;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Acesso;

using MediatR;

namespace Castwork.Application.Acesso.Commands.AlterarMinhaSenha;

/// <summary>
/// Troca a senha do usuário logado. Todas as outras sessões caem no próximo refresh (nova versão de segurança);
/// este dispositivo recebe uma sessão nova na resposta e continua logado.
/// </summary>
[SomenteAutenticado]
public sealed class AlterarMinhaSenhaCommand : IRequest<SessaoDto>
{
    public string SenhaAtual { get; set; } = string.Empty;

    public string NovaSenha { get; set; } = string.Empty;
}

public sealed class AlterarMinhaSenhaCommandHandler(
    IUnitOfWork unitOfWork,
    ISessaoService sessao,
    ISenhaHasher senhaHasher,
    EmissorDeSessao emissorDeSessao)
    : IRequestHandler<AlterarMinhaSenhaCommand, SessaoDto>
{
    public async Task<SessaoDto> Handle(AlterarMinhaSenhaCommand request, CancellationToken cancellationToken)
    {
        var usuario = await unitOfWork.GetRepository<Usuario>().ObterPorIdOuFalharAsync(sessao.UsuarioId!.Value, cancellationToken);

        if (senhaHasher.Verificar(usuario.SenhaHash, request.SenhaAtual) == ResultadoVerificacaoSenha.Invalida)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("senhaAtual", "Senha atual incorreta.")]);
        }

        usuario.AlterarSenha(senhaHasher.Hash(request.NovaSenha));
        var novaSessao = await emissorDeSessao.EmitirAsync(usuario, familia: Guid.NewGuid(), cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);

        return novaSessao;
    }
}
