using Castwork.Application.Abstractions;
using Castwork.Application.Common.Pagination.Models;
using Castwork.Application.Common.Validators;

using Moq;

namespace Castwork.UnitTests.Application.Common.Validators;

public class ValidatorPaginadoTests
{
    [TestCase(null)]
    [TestCase("nome")]
    [TestCase("CriadoEm")]
    public async Task DeveAceitarOrdenacaoPorCampoDoDtoAsync(string? campo)
    {
        var resultado = await new ValidatorDeTeste().ValidateAsync(new QueryDeTeste { OrdenarPor = campo });

        resultado.IsValid.Should().BeTrue();
    }

    [TestCase("SenhaHash")]
    [TestCase("Cliente.Nome")]
    public async Task DeveRejeitarOrdenacaoPorCampoForaDoDtoAsync(string campo)
    {
        var resultado = await new ValidatorDeTeste().ValidateAsync(new QueryDeTeste { OrdenarPor = campo });

        resultado.Errors.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { PropertyName = "ordenarPor", ErrorMessage = $"Não é possível ordenar por '{campo}'." });
    }

    public sealed class QueryDeTeste : QueryPaginada;

    public sealed class DtoDeTeste
    {
        public string Nome { get; init; } = string.Empty;

        public DateTimeOffset CriadoEm { get; init; }
    }

    private sealed class ValidatorDeTeste() : ValidatorPaginado<QueryDeTeste, DtoDeTeste>(Mock.Of<IUnitOfWork>());
}
