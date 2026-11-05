using Castwork.Application.Projetos.Commands.AdicionarItem;
using Castwork.Application.Projetos.Commands.AlterarItem;
using Castwork.Application.Projetos.Commands.ArquivarVersao;
using Castwork.Application.Projetos.Commands.CriarVersao;
using Castwork.Application.Projetos.Commands.DesarquivarVersao;
using Castwork.Application.Projetos.Commands.DuplicarVersao;
using Castwork.Application.Projetos.Commands.PublicarVersao;
using Castwork.Application.Projetos.Commands.RemoverItem;
using Castwork.Application.Projetos.Commands.VincularCliente;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Projetos;

/// <summary>
/// Ciclo de vida de um projeto pelo pipeline real (validação, autorização, agregado e PostgreSQL).
/// </summary>
public class ProjetoCommandsTests : TesteDeIntegracao
{
    [Test]
    public async Task DeveCriarProjetoDoClienteComVersao1EmDesenvolvimentoAsync()
    {
        var clienteId = await Usar(new ClienteBuilder()).BuildAsync();
        var projetos = Usar(new ProjetoBuilder());
        var command = await projetos.DoCliente(clienteId).CustomizeCommand(c => c.Nome = "Colar Raízes").BuildCommandAsync();

        var id = await projetos.SendAsync(command);

        var projeto = await LerAsync(id);
        projeto.Should().BeEquivalentTo(new { Nome = "Colar Raízes", ClienteId = clienteId, CriadoEm = RelogioDeTeste.Inicio }, o => o.ExcludingMissingMembers());
        projeto.Versoes.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Numero = 1, Nome = "Versão 1", Status = StatusVersao.EmDesenvolvimento });
    }

    [Test]
    public async Task DeveRejeitarClienteInexistenteAoCriarProjetoAsync()
    {
        var projetos = Usar(new ProjetoBuilder()).DoCliente(999_999);
        var command = await projetos.BuildCommandAsync();

        var enviar = () => projetos.SendAsync(command);

        (await enviar.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { PropertyName = "clienteId", ErrorMessage = "Cliente não encontrado." });
    }

    [Test]
    public async Task DeveComporVersaoSomandoQuantidadeDaMesmaPecaAsync()
    {
        var (projetoId, v1) = await ProjetoAsync();
        var elo = await Usar(new PecaBuilder()).BuildAsync();
        var fecho = await Usar(new PecaBuilder()).BuildAsync();

        await Adicionar(projetoId, v1, elo, 8);
        await Adicionar(projetoId, v1, elo, 4, "ródio");
        await Adicionar(projetoId, v1, fecho, 1);

        var itens = (await LerAsync(projetoId)).Versoes.Single().Itens;
        itens.OrderBy(i => i.Id).Select(i => (i.PecaId, i.Quantidade, i.Observacao)).Should().Equal((elo, 12, "ródio"), (fecho, 1, (string?)null));
    }

    [Test]
    public async Task DeveRejeitarPecaInativaNaComposicaoAsync()
    {
        var (projetoId, v1) = await ProjetoAsync();
        var peca = await Usar(new PecaBuilder()).BuildAsync();
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.Where(p => p.Id == peca).ExecuteUpdateAsync(s => s.SetProperty(p => p.Ativo, false)));

        var adicionar = () => Adicionar(projetoId, v1, peca, 1);

        (await adicionar.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("pecaId");
    }

    [Test]
    public async Task DeveAlterarERemoverItensDaComposicaoAsync()
    {
        var (projetoId, v1) = await ProjetoAsync();
        var elo = await Usar(new PecaBuilder()).BuildAsync();
        var fecho = await Usar(new PecaBuilder()).BuildAsync();
        var itemElo = await Adicionar(projetoId, v1, elo, 12);
        var itemFecho = await Adicionar(projetoId, v1, fecho, 1);

        await AmbienteDeTeste.SendAsync(new AlterarItemCommand { ProjetoId = projetoId, VersaoId = v1, ItemId = itemElo, Quantidade = 14, Observacao = "ajuste" });
        await AmbienteDeTeste.SendAsync(new RemoverItemCommand { ProjetoId = projetoId, VersaoId = v1, ItemId = itemFecho });

        (await LerAsync(projetoId)).Versoes.Single().Itens.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { PecaId = elo, Quantidade = 14, Observacao = "ajuste" });
    }

    [Test]
    public async Task DevePublicarCongelarEDuplicarVersaoAsync()
    {
        var (projetoId, v1) = await ProjetoAsync();
        var elo = await Usar(new PecaBuilder()).BuildAsync();
        await Adicionar(projetoId, v1, elo, 12);
        AmbienteDeTeste.Relogio.Avancar(TimeSpan.FromHours(1));

        await AmbienteDeTeste.SendAsync(new PublicarVersaoCommand { ProjetoId = projetoId, VersaoId = v1 });
        var alterarPublicada = () => Adicionar(projetoId, v1, elo, 1);
        var v2 = await AmbienteDeTeste.SendAsync(new DuplicarVersaoCommand { ProjetoId = projetoId, VersaoId = v1 });

        await alterarPublicada.Should().ThrowAsync<RegraDeNegocioException>().WithMessage("*publicada*");
        var versoes = (await LerAsync(projetoId)).Versoes.OrderBy(v => v.Numero).ToList();
        versoes.Select(v => new { v.Numero, v.Nome, v.Status, v.PublicadaEm }).Should().Equal(
            new { Numero = 1, Nome = "Versão 1", Status = StatusVersao.Publicada, PublicadaEm = (DateTimeOffset?)RelogioDeTeste.Inicio.AddHours(1) },
            new { Numero = 2, Nome = "Versão 2", Status = StatusVersao.EmDesenvolvimento, PublicadaEm = (DateTimeOffset?)null });
        versoes[1].Id.Should().Be(v2);
        versoes[1].Itens.Should().ContainSingle().Which.Should().BeEquivalentTo(new { PecaId = elo, Quantidade = 12 });
    }

    [Test]
    public async Task DeveRecusarPublicarVersaoVaziaAsync()
    {
        var (projetoId, v1) = await ProjetoAsync();

        var publicar = () => AmbienteDeTeste.SendAsync(new PublicarVersaoCommand { ProjetoId = projetoId, VersaoId = v1 });

        await publicar.Should().ThrowAsync<RegraDeNegocioException>().WithMessage("*ao menos uma peça*");
    }

    [Test]
    public async Task DeveCriarVersaoVaziaComProximoNumeroAsync()
    {
        var (projetoId, _) = await ProjetoAsync();

        var v2 = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Sem pedra", Observacoes = "mais leve" });

        (await LerAsync(projetoId)).Versoes.Single(v => v.Id == v2)
            .Should().BeEquivalentTo(new { Numero = 2, Nome = "Sem pedra", Observacoes = "mais leve", Status = StatusVersao.EmDesenvolvimento });
    }

    [Test]
    public async Task DeveArquivarEDesarquivarVersaoEmDesenvolvimentoAsync()
    {
        var (projetoId, v1) = await ProjetoAsync();

        await AmbienteDeTeste.SendAsync(new ArquivarVersaoCommand { ProjetoId = projetoId, VersaoId = v1 });
        var arquivada = (await LerAsync(projetoId)).Versoes.Single().Status;
        await AmbienteDeTeste.SendAsync(new DesarquivarVersaoCommand { ProjetoId = projetoId, VersaoId = v1 });

        arquivada.Should().Be(StatusVersao.Arquivada);
        (await LerAsync(projetoId)).Versoes.Single().Status.Should().Be(StatusVersao.EmDesenvolvimento);
    }

    [Test]
    public async Task DeveRecusarArquivarVersaoPublicadaAsync()
    {
        var (projetoId, v1) = await ProjetoAsync();
        await Adicionar(projetoId, v1, await Usar(new PecaBuilder()).BuildAsync(), 1);
        await AmbienteDeTeste.SendAsync(new PublicarVersaoCommand { ProjetoId = projetoId, VersaoId = v1 });

        var arquivar = () => AmbienteDeTeste.SendAsync(new ArquivarVersaoCommand { ProjetoId = projetoId, VersaoId = v1 });

        await arquivar.Should().ThrowAsync<RegraDeNegocioException>().WithMessage("*publicada*");
    }

    [Test]
    public async Task DeveVincularEDesvincularClienteDoProjetoAsync()
    {
        var (projetoId, _) = await ProjetoAsync();
        var clienteId = await Usar(new ClienteBuilder()).BuildAsync();

        await AmbienteDeTeste.SendAsync(new VincularClienteCommand { ProjetoId = projetoId, ClienteId = clienteId });
        var vinculado = (await LerAsync(projetoId)).ClienteId;
        await AmbienteDeTeste.SendAsync(new VincularClienteCommand { ProjetoId = projetoId, ClienteId = null });

        vinculado.Should().Be(clienteId);
        (await LerAsync(projetoId)).ClienteId.Should().BeNull();
    }

    [Test]
    public async Task DeveRejeitarVinculoComClienteInexistenteAsync()
    {
        var (projetoId, _) = await ProjetoAsync();

        var vincular = () => AmbienteDeTeste.SendAsync(new VincularClienteCommand { ProjetoId = projetoId, ClienteId = 999_999 });

        (await vincular.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { PropertyName = "clienteId", ErrorMessage = "Cliente não encontrado." });
    }

    [Test]
    public async Task DeveResponderNaoEncontradoParaVersaoDeOutroProjetoAsync()
    {
        var (projetoA, _) = await ProjetoAsync();
        var (_, versaoDoB) = await ProjetoAsync();
        var peca = await Usar(new PecaBuilder()).BuildAsync();

        var adicionar = () => Adicionar(projetoA, versaoDoB, peca, 1);

        await adicionar.Should().ThrowAsync<EntidadeNaoEncontradaException>();
    }

    private static Task<long> Adicionar(long projetoId, long versaoId, long pecaId, int quantidade, string? observacao = null)
        => AmbienteDeTeste.SendAsync(new AdicionarItemCommand { ProjetoId = projetoId, VersaoId = versaoId, PecaId = pecaId, Quantidade = quantidade, Observacao = observacao });

    private static Task<Projeto> LerAsync(long id)
        => AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Projetos.Include(p => p.Versoes).ThenInclude(v => v.Itens).AsNoTracking().SingleAsync(p => p.Id == id));

    private async Task<(long ProjetoId, long V1)> ProjetoAsync()
    {
        var projetos = Usar(new ProjetoBuilder());
        var id = await projetos.SendAsync(await projetos.BuildCommandAsync());
        return (id, (await LerAsync(id)).Versoes.Single().Id);
    }
}
