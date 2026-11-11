using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Castwork.Infra.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class LigaDasPecas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // O material das peças passa a ser uma liga do ateliê (Id + cópia do nome e do teor). As peças que já existem
            // ficam com o texto do material e o teor como cópia e liga_id nulo: a liga não é adivinhada pelo texto.
            migrationBuilder.AddColumn<long>(
                name: "liga_id",
                table: "projeto_pecas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "liga_id",
                table: "pecas",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_projeto_pecas_liga_id",
                table: "projeto_pecas",
                column: "liga_id");

            migrationBuilder.CreateIndex(
                name: "ix_pecas_liga_id",
                table: "pecas",
                column: "liga_id");

            migrationBuilder.AddForeignKey(
                name: "fk_pecas_ligas_liga_id",
                table: "pecas",
                column: "liga_id",
                principalTable: "ligas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_projeto_pecas_ligas_liga_id",
                table: "projeto_pecas",
                column: "liga_id",
                principalTable: "ligas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pecas_ligas_liga_id",
                table: "pecas");

            migrationBuilder.DropForeignKey(
                name: "fk_projeto_pecas_ligas_liga_id",
                table: "projeto_pecas");

            migrationBuilder.DropIndex(
                name: "ix_projeto_pecas_liga_id",
                table: "projeto_pecas");

            migrationBuilder.DropIndex(
                name: "ix_pecas_liga_id",
                table: "pecas");

            migrationBuilder.DropColumn(
                name: "liga_id",
                table: "projeto_pecas");

            migrationBuilder.DropColumn(
                name: "liga_id",
                table: "pecas");
        }
    }
}
