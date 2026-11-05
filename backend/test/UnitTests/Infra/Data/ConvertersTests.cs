using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.ValueObjects;
using Castwork.Infra.Data.Converters;

namespace Castwork.UnitTests.Infra.Data;

public class ConvertersTests
{
    [TestCase(StatusPeca.Briefing, "B")]
    [TestCase(StatusPeca.Acabamento, "F")]
    [TestCase(StatusPeca.PosVenda, "V")]
    [TestCase(StatusPeca.Cancelada, "X")]
    public void DeveConverterStatusPecaParaCodigoDeBancoEVoltar(StatusPeca status, string codigo)
    {
        var converter = new StatusPecaConverter();

        var resultado = new
        {
            Codigo = converter.ConvertToProvider(status),
            Status = converter.ConvertFromProvider(codigo),
        };

        resultado.Should().BeEquivalentTo(new { Codigo = codigo, Status = status });
    }

    [Test]
    public void DeveTerCodigoUnicoParaTodoStatusPeca()
    {
        var codigos = StatusPecaConverter.Codigos.Todos;

        codigos.Keys.Should().BeEquivalentTo(Enum.GetValues<StatusPeca>());
        codigos.Values.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void DeveRejeitarCodigoDeStatusDesconhecido()
    {
        var converter = new StatusPecaConverter();

        var decodificar = () => converter.ConvertFromProvider("Z");

        decodificar.Should().Throw<InvalidOperationException>().WithMessage("*'Z'*StatusPeca*");
    }

    [Test]
    public void DeveFalharAoMontarMapaSemCodigoParaTodosOsValores()
    {
        var criar = () => new CodigosDeEnum<StatusPeca>(new Dictionary<StatusPeca, char> { [StatusPeca.Briefing] = 'B' });

        criar.Should().Throw<InvalidOperationException>().WithMessage("*sem código*Orcamento*");
    }

    [Test]
    public void DeveConverterValueObjectsParaValorPrimitivoEVoltar()
    {
        var resultado = new
        {
            Dinheiro = new DinheiroConverter().ConvertFromProvider(new DinheiroConverter().ConvertToProvider(Dinheiro.Criar(10.5m))),
            Peso = new PesoConverter().ConvertFromProvider(new PesoConverter().ConvertToProvider(Peso.EmGramas(2.345m))),
            Teor = new TeorMetalConverter().ConvertFromProvider(new TeorMetalConverter().ConvertToProvider(TeorMetal.Ouro18k)),
            Documento = new DocumentoConverter().ConvertToProvider(Documento.Criar("529.982.247-25")),
        };

        resultado.Should().BeEquivalentTo(new
        {
            Dinheiro = Dinheiro.Criar(10.5m),
            Peso = Peso.EmGramas(2.345m),
            Teor = TeorMetal.Ouro18k,
            Documento = "52998224725",
        });
    }
}
