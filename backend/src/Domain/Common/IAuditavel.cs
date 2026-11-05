namespace Castwork.Domain.Common;

/// <summary>
/// Entidade com trilha de criação/alteração. Os valores são preenchidos pelo interceptor de auditoria
/// do EF, nunca pelo domínio; por isso a interface expõe só leitura.
/// </summary>
public interface IAuditavel
{
    DateTimeOffset CriadoEm { get; }

    long? CriadoPor { get; }

    DateTimeOffset? AlteradoEm { get; }

    long? AlteradoPor { get; }
}
