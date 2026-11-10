namespace Castwork.Application.Common.Security;

/// <summary>
/// Catálogo de regras (permissões) do sistema. Cada regra tem:
/// <list type="bullet">
/// <item>GUID: identidade estável (não muda entre instalações nem com renomeação), usado no <c>[Permissao]</c>;</item>
/// <item>código <c>&lt;dominio&gt;.&lt;acao&gt;</c>: vai no JWT (claim <c>permissoes</c>) para o front montar menu e guards;</item>
/// <item>descrição para a tela de perfis.</item>
/// </list>
/// Na inicialização o módulo Acesso sincroniza este catálogo com o banco. Nova regra: novo GUID (nunca reaproveitar um removido).
/// </summary>
public static class Regras
{
    public static class Clientes
    {
        [Regra("clientes.ler", "Consultar clientes")]
        public const string Consultar = "5b0f3c1e-8a52-4d7e-9a31-2f6c4e8d1a01";

        [Regra("clientes.criar", "Cadastrar clientes")]
        public const string Criar = "5b0f3c1e-8a52-4d7e-9a31-2f6c4e8d1a02";

        [Regra("clientes.editar", "Alterar clientes")]
        public const string Alterar = "5b0f3c1e-8a52-4d7e-9a31-2f6c4e8d1a03";
    }

    public static class Projetos
    {
        [Regra("projetos.ler", "Consultar projetos e versões")]
        public const string Consultar = "3f6a8c1d-7b24-4e9f-a1c3-5d7e9f0a1b01";

        [Regra("projetos.criar", "Criar projetos")]
        public const string Criar = "3f6a8c1d-7b24-4e9f-a1c3-5d7e9f0a1b02";

        [Regra("projetos.editar", "Criar versões, compor e publicar")]
        public const string Editar = "3f6a8c1d-7b24-4e9f-a1c3-5d7e9f0a1b03";
    }

    public static class Pecas
    {
        [Regra("pecas.ler", "Consultar a biblioteca de peças")]
        public const string Consultar = "7c2e9d4a-1f63-4b8c-8e42-3a7d5f9e2b01";

        [Regra("pecas.criar", "Cadastrar peças na biblioteca")]
        public const string Criar = "7c2e9d4a-1f63-4b8c-8e42-3a7d5f9e2b02";

        [Regra("pecas.editar", "Alterar peças da biblioteca")]
        public const string Alterar = "7c2e9d4a-1f63-4b8c-8e42-3a7d5f9e2b03";
    }

    public static class Precificacao
    {
        [Regra("precificacao.ler", "Consultar preços do ateliê, orçamentos e custos")]
        public const string Consultar = "4a1c7e9b-2d38-4f6a-a5b1-8c9d0e1f2a01";

        [Regra("precificacao.editar", "Alterar preços do ateliê e montar orçamentos")]
        public const string Editar = "4a1c7e9b-2d38-4f6a-a5b1-8c9d0e1f2a02";
    }

    public static class Documentos
    {
        [Regra("documentos.ler", "Gerar e baixar documentos em PDF")]
        public const string Consultar = "6e3b5d7f-9a12-4c4e-b6d8-0f1e2d3c4b01";
    }

    public static class Portfolio
    {
        [Regra("portfolio.ler", "Ver fotos do portfólio")]
        public const string Consultar = "8f5d7b9e-1c34-4e6a-8b0c-2d3e4f5a6b01";
    }

    public static class Acesso
    {
        [Regra("acesso.usuarios", "Gerenciar usuários")]
        public const string GerenciarUsuarios = "9d4a6b2c-3e71-4f5a-b8c9-1d2e3f4a5c01";

        [Regra("acesso.perfis", "Gerenciar perfis e permissões")]
        public const string GerenciarPerfis = "9d4a6b2c-3e71-4f5a-b8c9-1d2e3f4a5c02";
    }

    public static class Atelie
    {
        [Regra("atelie.configurar", "Configurar dados do ateliê")]
        public const string Configurar = "2e8f1a3b-5c6d-4e7f-9a0b-6c5d4e3f2a01";
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RegraAttribute(string codigo, string descricao) : Attribute
    {
        public string Codigo { get; } = codigo;

        public string Descricao { get; } = descricao;
    }
}
