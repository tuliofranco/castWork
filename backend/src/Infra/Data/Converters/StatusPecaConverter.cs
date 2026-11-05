using Castwork.Domain.Entities.Pecas;

namespace Castwork.Infra.Data.Converters;

public sealed class StatusPecaConverter() : EnumCharConverter<StatusPeca>(Codigos)
{
    public static readonly CodigosDeEnum<StatusPeca> Codigos = new(new Dictionary<StatusPeca, char>
    {
        [StatusPeca.Briefing] = 'B',
        [StatusPeca.Orcamento] = 'O',
        [StatusPeca.Aprovado] = 'A',
        [StatusPeca.Producao] = 'P',
        [StatusPeca.Acabamento] = 'F',
        [StatusPeca.Entregue] = 'E',
        [StatusPeca.PosVenda] = 'V',
        [StatusPeca.Cancelada] = 'X',
    });
}
