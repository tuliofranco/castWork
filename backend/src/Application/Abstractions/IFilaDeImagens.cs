namespace Castwork.Application.Abstractions;

/// <summary>
/// A fila das imagens a gerar: cada arquivo novo das ferramentas entra aqui depois de ir para o banco e é processado em
/// segundo plano, sem atrasar a resposta de quem salvou.
/// </summary>
public interface IFilaDeImagens
{
    void Enfileirar(long arquivoId);
}
