using Castwork.Domain.Common;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Atelie;

/// <summary>
/// Dados do ateliê (uma única linha por instalação): aparecem nos PDFs e servem de padrão na precificação.
/// A linha nasce na migration; aqui só se atualiza.
/// </summary>
public sealed partial class ConfiguracaoAtelie : BaseEntity<long>, IAggregateRoot, IAuditavel
{
    public const long IdUnico = 1;
    public const int NomeMaxLength = 150;
    public const int EmailMaxLength = 150;
    public const int TelefoneMaxLength = 20;
    public const int LogoMaxLength = 300;

    private ConfiguracaoAtelie()
    {
    }

    public string Nome { get; private set; } = string.Empty;

    /// <summary>
    /// CNPJ, ou CPF para quem ainda trabalha como pessoa física.
    /// </summary>
    public Documento? Documento { get; private set; }

    public string? Email { get; private set; }

    public string? Telefone { get; private set; }

    public Endereco? Endereco { get; private set; }

    /// <summary>
    /// Valor da hora de bancada sugerido ao precificar uma peça.
    /// </summary>
    public Dinheiro ValorHoraPadrao { get; private set; } = Dinheiro.Zero;

    /// <summary>
    /// Chave do arquivo do logo no storage (Etapa 10). Nulo usa só o nome nos PDFs.
    /// </summary>
    public string? LogoArquivo { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    public long? CriadoPor { get; private set; }

    public DateTimeOffset? AlteradoEm { get; private set; }

    public long? AlteradoPor { get; private set; }
}
