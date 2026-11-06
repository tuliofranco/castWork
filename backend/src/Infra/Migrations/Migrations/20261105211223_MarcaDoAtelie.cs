using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Castwork.Infra.Migrations
{
    /// <inheritdoc />
    public partial class MarcaDoAtelie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "logo_arquivo",
                table: "configuracao_atelie");

            migrationBuilder.AddColumn<string>(
                name: "assinatura",
                table: "configuracao_atelie",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "aviso_briefing",
                table: "configuracao_atelie",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cor_marca",
                table: "configuracao_atelie",
                type: "character(7)",
                fixedLength: true,
                maxLength: 7,
                nullable: false,
                defaultValue: "#2E6E68");

            migrationBuilder.AddColumn<string>(
                name: "instagram",
                table: "configuracao_atelie",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "logo",
                table: "configuracao_atelie",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_tipo",
                table: "configuracao_atelie",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "site",
                table: "configuracao_atelie",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "configuracao_atelie",
                keyColumn: "id",
                keyValue: 1L,
                columns: new[] { "assinatura", "aviso_briefing", "cor_marca", "instagram", "logo", "logo_tipo", "site" },
                values: new object[] { null, "Peça feita à mão: textura, tonalidade e proporções podem variar levemente em relação às referências.", "#2E6E68", null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "assinatura",
                table: "configuracao_atelie");

            migrationBuilder.DropColumn(
                name: "aviso_briefing",
                table: "configuracao_atelie");

            migrationBuilder.DropColumn(
                name: "cor_marca",
                table: "configuracao_atelie");

            migrationBuilder.DropColumn(
                name: "instagram",
                table: "configuracao_atelie");

            migrationBuilder.DropColumn(
                name: "logo",
                table: "configuracao_atelie");

            migrationBuilder.DropColumn(
                name: "logo_tipo",
                table: "configuracao_atelie");

            migrationBuilder.DropColumn(
                name: "site",
                table: "configuracao_atelie");

            migrationBuilder.AddColumn<string>(
                name: "logo_arquivo",
                table: "configuracao_atelie",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "configuracao_atelie",
                keyColumn: "id",
                keyValue: 1L,
                column: "logo_arquivo",
                value: null);
        }
    }
}
