namespace Castwork.Application.Abstractions;

public enum ResultadoVerificacaoSenha
{
    Invalida,
    Valida,

    /// <summary>
    /// Senha correta, mas o hash foi gerado com parâmetros antigos: deve ser regravado.
    /// </summary>
    ValidaPrecisaRehash,
}

public interface ISenhaHasher
{
    string Hash(string senha);

    ResultadoVerificacaoSenha Verificar(string hash, string senha);

    /// <summary>
    /// Gasta o mesmo tempo de uma verificação real. Usado quando o usuário não existe, para que o tempo
    /// de resposta do login não revele quais e-mails estão cadastrados.
    /// </summary>
    void SimularVerificacao(string senha);
}
