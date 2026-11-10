using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Castwork.Infra.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ArquivoFinalDasPecas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // O desenho de cada peça do projeto passa a ser o arquivo final escolhido pelo joalheiro (arquivo_final_id), e
            // não mais o mais recente. As peças que já têm desenho ficam com o arquivo atual (o mais recente não arquivado
            // e não em branco) como final: nada muda para elas. Conversas da versão e do projeto ficam sem final.
            migrationBuilder.AddColumn<long>(
                name: "arquivo_final_id",
                table: "ferramenta_conversas",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_ferramenta_conversas_arquivo_final_id",
                table: "ferramenta_conversas",
                column: "arquivo_final_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ferramenta_conversas_ferramenta_arquivos_arquivo_final_id",
                table: "ferramenta_conversas",
                column: "arquivo_final_id",
                principalTable: "ferramenta_arquivos",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
                UPDATE ferramenta_conversas c
                SET arquivo_final_id = (
                    SELECT a.id FROM ferramenta_arquivos a
                    WHERE a.conversa_id = c.id AND a.arquivado_em IS NULL
                    ORDER BY a.id DESC
                    LIMIT 1)
                WHERE c.peca_do_projeto_id IS NOT NULL;

                UPDATE ferramenta_conversas c
                SET arquivo_final_id = NULL
                FROM ferramenta_arquivos a
                WHERE a.id = c.arquivo_final_id AND octet_length(a.conteudo) = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ferramenta_conversas_ferramenta_arquivos_arquivo_final_id",
                table: "ferramenta_conversas");

            migrationBuilder.DropIndex(
                name: "ix_ferramenta_conversas_arquivo_final_id",
                table: "ferramenta_conversas");

            migrationBuilder.DropColumn(
                name: "arquivo_final_id",
                table: "ferramenta_conversas");
        }
    }
}
