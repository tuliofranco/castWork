using Castwork.Domain.ValueObjects;
using Castwork.Infra.Data.Converters;

namespace Castwork.UnitTests.Infra.Data;

public class ConvertersTests
{
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
