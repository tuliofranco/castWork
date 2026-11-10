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
    public const int AssinaturaMaxLength = 150;
    public const int InstagramMaxLength = 60;
    public const int SiteMaxLength = 150;
    public const int AvisoMaxLength = 500;
    public const int LogoTamanhoMaximo = 1024 * 1024;
    public const string CorPadrao = "#2E6E68";
    public const string AvisoPadrao = "Peça feita à mão: textura, tonalidade e proporções podem variar levemente em relação às referências.";

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
    /// Padrões de preço do ateliê (perda, valor da hora, markup, taxas, arredondamento). Alterados pela tela de
    /// preços, com a regra de precificação, e não junto com os dados e a marca.
    /// </summary>
    public ParametrosDePreco ParametrosDePreco { get; private set; } = null!;

    /// <summary>
    /// Frase curta que acompanha o nome nos documentos ("Joias autorais feitas à mão").
    /// </summary>
    public string? Assinatura { get; private set; }

    /// <summary>
    /// Cor da marca em hexadecimal (#RRGGBB): linhas e títulos de seção dos documentos.
    /// </summary>
    public string CorMarca { get; private set; } = CorPadrao;

    public string? Instagram { get; private set; }

    public string? Site { get; private set; }

    /// <summary>
    /// Aviso padrão no fim do briefing (ex.: variações de peça feita à mão). Nulo omite a seção.
    /// </summary>
    public string? AvisoBriefing { get; private set; }

    /// <summary>
    /// Logo (PNG ou JPEG) guardado no banco: é pequeno e só um por instalação. Nulo usa só o nome nos PDFs.
    /// </summary>
    public byte[]? Logo { get; private set; }

    public string? LogoTipo { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    public long? CriadoPor { get; private set; }

    public DateTimeOffset? AlteradoEm { get; private set; }

    public long? AlteradoPor { get; private set; }
}
