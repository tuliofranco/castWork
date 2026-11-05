using Castwork.Application.Abstractions;
using Castwork.Application.Acesso.Common;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Acesso;

using MediatR;

namespace Castwork.Application.Acesso.Commands.Login;

[SemPermissao]
public sealed class LoginCommand : IRequest<SessaoDto>
{
    public string Email { get; set; } = string.Empty;

    public string Senha { get; set; } = string.Empty;
}

/// <summary>
/// Toda falha responde a mesma mensagem (usuário inexistente, inativo, bloqueado ou senha errada):
/// o login não pode servir para descobrir quais e-mails existem nem quais contas estão bloqueadas.
/// </summary>
public sealed class LoginCommandHandler(
    IUnitOfWork unitOfWork,
    ISenhaHasher senhaHasher,
    EmissorDeSessao emissorDeSessao,
    TimeProvider timeProvider)
    : IRequestHandler<LoginCommand, SessaoDto>
{
    public const string MensagemFalha = "Usuário ou senha inválidos.";

    public async Task<SessaoDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var agora = timeProvider.GetUtcNow();
        var usuario = await ObterUsuarioAsync(request.Email, cancellationToken);

        if (usuario is null || !usuario.Ativo || usuario.EstaBloqueado(agora))
        {
            senhaHasher.SimularVerificacao(request.Senha);
            throw new UnauthorizedAccessException(MensagemFalha);
        }

        var resultado = senhaHasher.Verificar(usuario.SenhaHash, request.Senha);
        if (resultado == ResultadoVerificacaoSenha.Invalida)
        {
            usuario.RegistrarFalhaDeLogin(agora);
            await unitOfWork.SalvarAsync(cancellationToken);
            throw new UnauthorizedAccessException(MensagemFalha);
        }

        if (resultado == ResultadoVerificacaoSenha.ValidaPrecisaRehash)
        {
            usuario.AtualizarHashDaSenha(senhaHasher.Hash(request.Senha));
        }

        usuario.RegistrarLogin(agora);
        var sessao = await emissorDeSessao.EmitirAsync(usuario, familia: Guid.NewGuid(), cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);

        return sessao;
    }

    private Task<Usuario?> ObterUsuarioAsync(string email, CancellationToken cancellationToken)
        => string.IsNullOrWhiteSpace(email)
            ? Task.FromResult<Usuario?>(null)
            : unitOfWork.GetRepository<Usuario>()
                .FindBy(u => u.Email == Usuario.NormalizarEmail(email))
                .SingleOrDefaultAsync(cancellationToken);
}
