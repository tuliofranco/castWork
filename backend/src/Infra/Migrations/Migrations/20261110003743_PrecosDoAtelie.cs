using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Castwork.Infra.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class PrecosDoAtelie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "valor_hora_padrao",
                table: "configuracao_atelie",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "arredondar_para",
                table: "configuracao_atelie",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 10m);

            migrationBuilder.AddColumn<decimal>(
                name: "markup",
                table: "configuracao_atelie",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 1.8m);

            migrationBuilder.AddColumn<decimal>(
                name: "perda_percentual",
                table: "configuracao_atelie",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 8m);

            migrationBuilder.AddColumn<decimal>(
                name: "taxas_percentual",
                table: "configuracao_atelie",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "cotacoes_metal",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    metal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    registrada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    registrada_por = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cotacoes_metal", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ligas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    metal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    teor = table.Column<int>(type: "integer", nullable: false),
                    densidade = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<long>(type: "bigint", nullable: true),
                    alterado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    alterado_por = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ligas", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "ligas",
                columns: new[] { "id", "alterado_em", "alterado_por", "ativo", "criado_em", "criado_por", "densidade", "metal", "nome", "teor" },
                values: new object[,]
                {
                    { 1L, null, null, true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 19.3m, "Ouro", "Ouro 24k", 999 },
                    { 2L, null, null, true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 15.5m, "Ouro", "Ouro 18k amarelo", 750 },
                    { 3L, null, null, true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 15.0m, "Ouro", "Ouro 18k branco", 750 },
                    { 4L, null, null, true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 15.1m, "Ouro", "Ouro 18k rosé", 750 },
                    { 5L, null, null, true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 13.4m, "Ouro", "Ouro 14k amarelo", 585 },
                    { 6L, null, null, true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 10.4m, "Prata", "Prata 950", 950 },
                    { 7L, null, null, true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 10.36m, "Prata", "Prata 925", 925 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_cotacoes_metal_metal_registrada_em",
                table: "cotacoes_metal",
                columns: new[] { "metal", "registrada_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_ligas_nome",
                table: "ligas",
                column: "nome");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cotacoes_metal");

            migrationBuilder.DropTable(
                name: "ligas");

            migrationBuilder.DropColumn(
                name: "arredondar_para",
                table: "configuracao_atelie");

            migrationBuilder.DropColumn(
                name: "markup",
                table: "configuracao_atelie");

            migrationBuilder.DropColumn(
                name: "perda_percentual",
                table: "configuracao_atelie");

            migrationBuilder.DropColumn(
                name: "taxas_percentual",
                table: "configuracao_atelie");

            migrationBuilder.AlterColumn<decimal>(
                name: "valor_hora_padrao",
                table: "configuracao_atelie",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "configuracao_atelie",
                keyColumn: "id",
                keyValue: 1L,
                column: "valor_hora_padrao",
                value: 0m);
        }
    }
}
