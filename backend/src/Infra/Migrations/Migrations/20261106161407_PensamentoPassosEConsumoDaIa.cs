using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Castwork.Infra.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class PensamentoPassosEConsumoDaIa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "passos",
                table: "ferramenta_mensagens",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "pensamento",
                table: "ferramenta_mensagens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tokens",
                table: "ferramenta_mensagens",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ia_consumo",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    origem = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    conversa_id = table.Column<long>(type: "bigint", nullable: false),
                    modelo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    chamadas = table.Column<int>(type: "integer", nullable: false),
                    entrada = table.Column<int>(type: "integer", nullable: false),
                    saida = table.Column<int>(type: "integer", nullable: false),
                    cache_leitura = table.Column<int>(type: "integer", nullable: false),
                    cache_escrita = table.Column<int>(type: "integer", nullable: false),
                    registrado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ia_consumo", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ia_consumo_origem_conversa_id",
                table: "ia_consumo",
                columns: new[] { "origem", "conversa_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ia_consumo_registrado_em",
                table: "ia_consumo",
                column: "registrado_em");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ia_consumo");

            migrationBuilder.DropColumn(
                name: "passos",
                table: "ferramenta_mensagens");

            migrationBuilder.DropColumn(
                name: "pensamento",
                table: "ferramenta_mensagens");

            migrationBuilder.DropColumn(
                name: "tokens",
                table: "ferramenta_mensagens");
        }
    }
}
