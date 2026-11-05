using Castwork.Domain.Entities.Acesso;
using Castwork.Domain.Exceptions;

namespace Castwork.UnitTests.Domain.Entities.Acesso;

public class UsuarioTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 13, 0, 0, TimeSpan.Zero);

    [Test]
    public void DeveCriarUsuarioAtivoComEmailNormalizado()
    {
        var usuario = Usuario.Criar(" Ana ", "  Ana@Atelie.COM ", "hash", perfilId: 3, isAdmin: false);

        usuario.Should().BeEquivalentTo(new { Nome = "Ana", Email = "ana@atelie.com", PerfilId = 3L, IsAdmin = false, Ativo = true, TentativasFalhas = 0 });
        usuario.VersaoSeguranca.Should().NotBeEmpty();
    }

    [Test]
    public void DeveExigirPerfilParaUsuarioQueNaoEAdmin()
    {
        var criar = () => Usuario.Criar("Ana", "ana@atelie.com", "hash", perfilId: null, isAdmin: false);

        criar.Should().Throw<RegraDeNegocioException>();
    }

    [Test]
    public void DeveBloquearUsuarioNaQuintaFalhaSeguidaPorQuinzeMinutos()
    {
        var usuario = NovoUsuario();

        for (var i = 0; i < Usuario.MaximoDeTentativas; i++)
        {
            usuario.RegistrarFalhaDeLogin(Agora);
        }

        usuario.Should().BeEquivalentTo(new { BloqueadoAte = (DateTimeOffset?)Agora.AddMinutes(15), TentativasFalhas = 0 });
        usuario.EstaBloqueado(Agora.AddMinutes(14)).Should().BeTrue();
        usuario.EstaBloqueado(Agora.AddMinutes(15)).Should().BeFalse();
    }

    [Test]
    public void DeveZerarTentativasAoRegistrarLogin()
    {
        var usuario = NovoUsuario();
        usuario.RegistrarFalhaDeLogin(Agora);
        usuario.RegistrarFalhaDeLogin(Agora);

        usuario.RegistrarLogin(Agora.AddMinutes(1));

        usuario.Should().BeEquivalentTo(new { TentativasFalhas = 0, BloqueadoAte = (DateTimeOffset?)null, UltimoLoginEm = (DateTimeOffset?)Agora.AddMinutes(1) });
    }

    [Test]
    public void DeveTrocarVersaoDeSegurancaAoAlterarSenhaPerfilOuDesativar()
    {
        var usuario = NovoUsuario();
        var versoes = new List<Guid> { usuario.VersaoSeguranca };

        usuario.AlterarSenha("novo-hash");
        versoes.Add(usuario.VersaoSeguranca);
        usuario.AlterarPerfil(perfilId: null, isAdmin: true);
        versoes.Add(usuario.VersaoSeguranca);
        usuario.Desativar();
        versoes.Add(usuario.VersaoSeguranca);

        versoes.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void DeveManterVersaoDeSegurancaNoRehashDaMesmaSenha()
    {
        var usuario = NovoUsuario();
        var versao = usuario.VersaoSeguranca;

        usuario.AtualizarHashDaSenha("hash-mais-forte");

        usuario.Should().BeEquivalentTo(new { SenhaHash = "hash-mais-forte", VersaoSeguranca = versao });
    }

    private static Usuario NovoUsuario() => Usuario.Criar("Ana", "ana@atelie.com", "hash", perfilId: 1, isAdmin: false);
}
