using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Castwork.Infra.Migrations
{
    /// <inheritdoc />
    public partial class FerramentasComIa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ferramenta_conversas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    versao_id = table.Column<long>(type: "bigint", nullable: false),
                    ferramenta = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ferramenta_conversas", x => x.id);
                    table.ForeignKey(
                        name: "fk_ferramenta_conversas_versao_projeto_versao_id",
                        column: x => x.versao_id,
                        principalTable: "projeto_versoes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ferramenta_arquivos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    conversa_id = table.Column<long>(type: "bigint", nullable: false),
                    conteudo = table.Column<byte[]>(type: "bytea", nullable: false),
                    origem = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ferramenta_arquivos", x => x.id);
                    table.ForeignKey(
                        name: "fk_ferramenta_arquivos_conversas_ferramenta_conversa_id",
                        column: x => x.conversa_id,
                        principalTable: "ferramenta_conversas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ferramenta_imagens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    conversa_id = table.Column<long>(type: "bigint", nullable: false),
                    chave = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    conteudo = table.Column<byte[]>(type: "bytea", nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ferramenta_imagens", x => x.id);
                    table.ForeignKey(
                        name: "fk_ferramenta_imagens_ferramenta_conversas_conversa_id",
                        column: x => x.conversa_id,
                        principalTable: "ferramenta_conversas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ferramenta_mensagens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    papel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    texto = table.Column<string>(type: "text", nullable: true),
                    conteudo = table.Column<string>(type: "jsonb", nullable: false),
                    imagens = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    acao_descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    acao_arquivo_antes_id = table.Column<long>(type: "bigint", nullable: true),
                    acao_desfeita = table.Column<bool>(type: "boolean", nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    conversa_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ferramenta_mensagens", x => x.id);
                    table.ForeignKey(
                        name: "fk_ferramenta_mensagens_ferramenta_conversas_conversa_id",
                        column: x => x.conversa_id,
                        principalTable: "ferramenta_conversas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ferramenta_arquivos_conversa_id_id",
                table: "ferramenta_arquivos",
                columns: new[] { "conversa_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_ferramenta_conversas_versao_id_ferramenta",
                table: "ferramenta_conversas",
                columns: new[] { "versao_id", "ferramenta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ferramenta_imagens_chave",
                table: "ferramenta_imagens",
                column: "chave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ferramenta_imagens_conversa_id",
                table: "ferramenta_imagens",
                column: "conversa_id");

            migrationBuilder.CreateIndex(
                name: "ix_ferramenta_mensagens_conversa_id_ordem",
                table: "ferramenta_mensagens",
                columns: new[] { "conversa_id", "ordem" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ferramenta_arquivos");

            migrationBuilder.DropTable(
                name: "ferramenta_imagens");

            migrationBuilder.DropTable(
                name: "ferramenta_mensagens");

            migrationBuilder.DropTable(
                name: "ferramenta_conversas");
        }
    }
}
