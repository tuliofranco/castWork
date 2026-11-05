using Castwork.Application.Abstractions;
using Castwork.Application.Pecas.Queries.GetPecas;

using Moq;

namespace Castwork.UnitTests.Application.Pecas;

public class GetPecasQueryValidatorTests
{
    [TestCase(null)]
    [TestCase("titulo")]
    [TestCase("ClienteNome")]
    public async Task DeveAceitarOrdenacaoPorCampoDoDtoAsync(string? campo)
    {
        var validator = new GetPecasQueryValidator(Mock.Of<IUnitOfWork>());

        var resultado = await validator.ValidateAsync(new GetPecasQuery { OrdenarPor = campo });

        resultado.IsValid.Should().BeTrue();
    }

    [TestCase("Descricao")]
    [TestCase("CriadoPor")]
    [TestCase("Cliente.Nome")]
    public async Task DeveRejeitarOrdenacaoPorCampoForaDoDtoAsync(string campo)
    {
        var validator = new GetPecasQueryValidator(Mock.Of<IUnitOfWork>());

        var resultado = await validator.ValidateAsync(new GetPecasQuery { OrdenarPor = campo });

        resultado.Errors.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { PropertyName = "ordenarPor", ErrorMessage = $"Não é possível ordenar por '{campo}'." });
    }
}
