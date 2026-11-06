namespace Castwork.Application.Common.Exceptions;

/// <summary>
/// Um serviço externo (IA, gerador de documentos) não está configurado ou não respondeu. Vira 503.
/// A mensagem é segura para o usuário.
/// </summary>
public sealed class ServicoIndisponivelException(string message, Exception? innerException = null)
    : Exception(message, innerException);
