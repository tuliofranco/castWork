using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Castwork.Infra.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class OrcamentoDaVersao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "mao_de_obra_horas",
                table: "projeto_versoes",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mao_de_obra_tipo",
                table: "projeto_versoes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "mao_de_obra_valor_fixo",
                table: "projeto_versoes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "markup",
                table: "projeto_versoes",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metais",
                table: "projeto_versoes",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "servicos",
                table: "projeto_versoes",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mao_de_obra_horas",
                table: "projeto_versoes");

            migrationBuilder.DropColumn(
                name: "mao_de_obra_tipo",
                table: "projeto_versoes");

            migrationBuilder.DropColumn(
                name: "mao_de_obra_valor_fixo",
                table: "projeto_versoes");

            migrationBuilder.DropColumn(
                name: "markup",
                table: "projeto_versoes");

            migrationBuilder.DropColumn(
                name: "metais",
                table: "projeto_versoes");

            migrationBuilder.DropColumn(
                name: "servicos",
                table: "projeto_versoes");
        }
    }
}
