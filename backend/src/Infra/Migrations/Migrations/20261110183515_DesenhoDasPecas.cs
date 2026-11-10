using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Castwork.Infra.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class DesenhoDasPecas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cada peça do projeto ganha o próprio desenho 2D e modelo 3D: a conversa da ferramenta pode ser da peça
            // (peca_do_projeto_id), e a unicidade passa a contar a peça. As conversas que já existem são do projeto ou da
            // versão e ficam como estão (peca_do_projeto_id nulo). A biblioteca guarda o desenho copiado ao publicar
            // (peca_desenhos); as peças que já estão nela ficam sem desenho.
            migrationBuilder.DropIndex(
                name: "ix_ferramenta_conversas_projeto_id_versao_id_ferramenta",
                table: "ferramenta_conversas");

            migrationBuilder.AddColumn<long>(
                name: "peca_do_projeto_id",
                table: "ferramenta_conversas",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "peca_desenhos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ferramenta = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    conteudo = table.Column<byte[]>(type: "bytea", nullable: false),
                    copiado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    peca_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_peca_desenhos", x => x.id);
                    table.ForeignKey(
                        name: "fk_peca_desenhos_pecas_peca_id",
                        column: x => x.peca_id,
                        principalTable: "pecas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ferramenta_conversas_peca_do_projeto_id",
                table: "ferramenta_conversas",
                column: "peca_do_projeto_id");

            migrationBuilder.CreateIndex(
                name: "ix_ferramenta_conversas_projeto_id_versao_id_peca_do_projeto_i",
                table: "ferramenta_conversas",
                columns: new[] { "projeto_id", "versao_id", "peca_do_projeto_id", "ferramenta" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_peca_desenhos_peca_id_ferramenta",
                table: "peca_desenhos",
                columns: new[] { "peca_id", "ferramenta" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_ferramenta_conversas_peca_do_projeto_peca_do_projeto_id",
                table: "ferramenta_conversas",
                column: "peca_do_projeto_id",
                principalTable: "projeto_pecas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // As conversas das peças não cabem no formato antigo (seriam do projeto, repetidas por ferramenta): saem.
            migrationBuilder.Sql("DELETE FROM ferramenta_conversas WHERE peca_do_projeto_id IS NOT NULL;");

            migrationBuilder.DropForeignKey(
                name: "fk_ferramenta_conversas_peca_do_projeto_peca_do_projeto_id",
                table: "ferramenta_conversas");

            migrationBuilder.DropTable(
                name: "peca_desenhos");

            migrationBuilder.DropIndex(
                name: "ix_ferramenta_conversas_peca_do_projeto_id",
                table: "ferramenta_conversas");

            migrationBuilder.DropIndex(
                name: "ix_ferramenta_conversas_projeto_id_versao_id_peca_do_projeto_i",
                table: "ferramenta_conversas");

            migrationBuilder.DropColumn(
                name: "peca_do_projeto_id",
                table: "ferramenta_conversas");

            migrationBuilder.CreateIndex(
                name: "ix_ferramenta_conversas_projeto_id_versao_id_ferramenta",
                table: "ferramenta_conversas",
                columns: new[] { "projeto_id", "versao_id", "ferramenta" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }
    }
}
