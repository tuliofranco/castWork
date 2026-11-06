using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Castwork.Infra.Migrations
{
    /// <inheritdoc />
    public partial class ImagensDoBriefing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid[]>(
                name: "imagens",
                table: "briefing_mensagens",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);

            migrationBuilder.CreateTable(
                name: "briefing_imagens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    briefing_id = table.Column<long>(type: "bigint", nullable: false),
                    chave = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    conteudo = table.Column<byte[]>(type: "bytea", nullable: false),
                    enviada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_briefing_imagens", x => x.id);
                    table.ForeignKey(
                        name: "fk_briefing_imagens_briefings_briefing_id",
                        column: x => x.briefing_id,
                        principalTable: "briefings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_briefing_imagens_briefing_id",
                table: "briefing_imagens",
                column: "briefing_id");

            migrationBuilder.CreateIndex(
                name: "ix_briefing_imagens_chave",
                table: "briefing_imagens",
                column: "chave",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "briefing_imagens");

            migrationBuilder.DropColumn(
                name: "imagens",
                table: "briefing_mensagens");
        }
    }
}
