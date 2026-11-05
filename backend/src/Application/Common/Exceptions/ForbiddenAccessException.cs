namespace Castwork.Application.Common.Exceptions;

/// <summary>
/// Usuário autenticado, mas sem a regra exigida pelo request. A API traduz para 403.
/// (Sem usuário, a exceção é <see cref="UnauthorizedAccessException"/>, que vira 401.)
/// </summary>
public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException()
        : base("Você não tem permissão para executar esta ação.")
    {
    }

    public ForbiddenAccessException(string message)
        : base(message)
    {
    }

    public ForbiddenAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
