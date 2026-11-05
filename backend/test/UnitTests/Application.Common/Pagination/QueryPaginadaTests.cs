using Castwork.Application.Common.Pagination.Models;

namespace Castwork.UnitTests.Application.Common.Pagination;

public class QueryPaginadaTests
{
    [TestCase(0, 0, 1, 50)]
    [TestCase(-3, 51, 1, 50)]
    [TestCase(2, 10, 2, 10)]
    public void DeveCorrigirPaginaETamanhoForaDaFaixa(int pagina, int tamanho, int paginaEsperada, int tamanhoEsperado)
    {
        var query = new QueryDeTeste { Pagina = pagina, TamanhoPagina = tamanho };

        query.Should().BeEquivalentTo(new { Pagina = paginaEsperada, TamanhoPagina = tamanhoEsperado });
    }

    private sealed class QueryDeTeste : QueryPaginada;
}
