using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Castwork.Infra.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class PrecosDoProjeto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "arredondar_para",
                table: "projetos",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "markup",
                table: "projetos",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "perda_percentual",
                table: "projetos",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "precos_alterados_em",
                table: "projetos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "precos_herdados_em",
                table: "projetos",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<decimal>(
                name: "taxas_percentual",
                table: "projetos",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_hora",
                table: "projetos",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "projeto_cotacoes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    metal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    cotada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    projeto_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projeto_cotacoes", x => x.id);
                    table.ForeignKey(
                        name: "fk_projeto_cotacoes_projetos_projeto_id",
                        column: x => x.projeto_id,
                        principalTable: "projetos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_projeto_cotacoes_projeto_id_metal",
                table: "projeto_cotacoes",
                columns: new[] { "projeto_id", "metal" },
                unique: true);

            // Projetos que já existem herdam os preços atuais do ateliê, com a data desta migration: os parâmetros da
            // linha do ateliê e a cotação vigente (a mais recente) de cada metal já cotado.
            migrationBuilder.Sql("""
                UPDATE projetos p
                SET perda_percentual = a.perda_percentual,
                    valor_hora = a.valor_hora_padrao,
                    markup = a.markup,
                    taxas_percentual = a.taxas_percentual,
                    arredondar_para = a.arredondar_para,
                    precos_herdados_em = now()
                FROM configuracao_atelie a
                WHERE a.id = 1;

                INSERT INTO projeto_cotacoes (projeto_id, metal, valor, cotada_em)
                SELECT p.id, c.metal, c.valor, c.registrada_em
                FROM projetos p
                CROSS JOIN (
                    SELECT DISTINCT ON (metal) metal, valor, registrada_em
                    FROM cotacoes_metal
                    ORDER BY metal, registrada_em DESC, id DESC
                ) c;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "projeto_cotacoes");

            migrationBuilder.DropColumn(
                name: "arredondar_para",
                table: "projetos");

            migrationBuilder.DropColumn(
                name: "markup",
                table: "projetos");

            migrationBuilder.DropColumn(
                name: "perda_percentual",
                table: "projetos");

            migrationBuilder.DropColumn(
                name: "precos_alterados_em",
                table: "projetos");

            migrationBuilder.DropColumn(
                name: "precos_herdados_em",
                table: "projetos");

            migrationBuilder.DropColumn(
                name: "taxas_percentual",
                table: "projetos");

            migrationBuilder.DropColumn(
                name: "valor_hora",
                table: "projetos");
        }
    }
}
