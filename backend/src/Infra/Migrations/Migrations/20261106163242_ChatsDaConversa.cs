using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Castwork.Infra.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ChatsDaConversa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // As conversas que já existem viram o chat 1 (o em andamento).
            migrationBuilder.AddColumn<int>(
                name: "chat",
                table: "ferramenta_mensagens",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "chat_atual",
                table: "ferramenta_conversas",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "chat",
                table: "ferramenta_mensagens");

            migrationBuilder.DropColumn(
                name: "chat_atual",
                table: "ferramenta_conversas");
        }
    }
}
