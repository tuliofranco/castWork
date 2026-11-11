using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Castwork.Infra.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class PecasDoProjeto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "projeto_pecas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    material = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    teor = table.Column<int>(type: "integer", nullable: true),
                    peso = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    peca_id = table.Column<long>(type: "bigint", nullable: true),
                    projeto_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projeto_pecas", x => x.id);
                    table.ForeignKey(
                        name: "fk_projeto_pecas_pecas_peca_id",
                        column: x => x.peca_id,
                        principalTable: "pecas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_projeto_pecas_projetos_projeto_id",
                        column: x => x.projeto_id,
                        principalTable: "projetos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_projeto_pecas_peca_id",
                table: "projeto_pecas",
                column: "peca_id");

            migrationBuilder.CreateIndex(
                name: "ix_projeto_pecas_projeto_id",
                table: "projeto_pecas",
                column: "projeto_id");

            migrationBuilder.DropForeignKey(
                name: "fk_projeto_versao_itens_pecas_peca_id",
                table: "projeto_versao_itens");

            // Coluna nova (e não renomear a antiga): trocar os Ids no lugar poderia repetir por um instante o par
            // (versão, peça) do índice único.
            migrationBuilder.AddColumn<long>(
                name: "peca_do_projeto_id",
                table: "projeto_versao_itens",
                type: "bigint",
                nullable: true);

            // Cada peça da biblioteca usada numa versão do projeto vira uma peça do projeto (uma por projeto, a mesma em
            // todas as versões dele): publicada, vinculada à original e com o custo atual dela como valor. As versões já
            // publicadas não são conferidas de novo.
            migrationBuilder.Sql("""
                INSERT INTO projeto_pecas (projeto_id, peca_id, nome, tipo, material, teor, peso, descricao, ativo, valor, status)
                SELECT DISTINCT v.projeto_id, p.id, p.nome, p.tipo, p.material, p.teor, p.peso, p.descricao, p.ativo, p.custo, 'Publicada'
                FROM projeto_versao_itens i
                JOIN projeto_versoes v ON v.id = i.versao_id
                JOIN pecas p ON p.id = i.peca_id
                ORDER BY v.projeto_id, p.id;

                UPDATE projeto_versao_itens i
                SET peca_do_projeto_id = pp.id
                FROM projeto_versoes v
                JOIN projeto_pecas pp ON pp.projeto_id = v.projeto_id
                WHERE v.id = i.versao_id AND pp.peca_id = i.peca_id;
                """);

            migrationBuilder.DropIndex(
                name: "ix_projeto_versao_itens_versao_id_peca_id",
                table: "projeto_versao_itens");

            migrationBuilder.DropIndex(
                name: "ix_projeto_versao_itens_peca_id",
                table: "projeto_versao_itens");

            migrationBuilder.DropColumn(
                name: "peca_id",
                table: "projeto_versao_itens");

            migrationBuilder.AlterColumn<long>(
                name: "peca_do_projeto_id",
                table: "projeto_versao_itens",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_projeto_versao_itens_peca_do_projeto_id",
                table: "projeto_versao_itens",
                column: "peca_do_projeto_id");

            migrationBuilder.CreateIndex(
                name: "ix_projeto_versao_itens_versao_id_peca_do_projeto_id",
                table: "projeto_versao_itens",
                columns: new[] { "versao_id", "peca_do_projeto_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_projeto_versao_itens_peca_do_projeto_peca_do_projeto_id",
                table: "projeto_versao_itens",
                column: "peca_do_projeto_id",
                principalTable: "projeto_pecas",
                principalColumn: "id");

            // O custo já foi copiado para o valor das peças do projeto: a biblioteca não tem valor.
            migrationBuilder.DropColumn(
                name: "custo",
                table: "pecas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_projeto_versao_itens_peca_do_projeto_peca_do_projeto_id",
                table: "projeto_versao_itens");

            migrationBuilder.AddColumn<decimal>(
                name: "custo",
                table: "pecas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "peca_id",
                table: "projeto_versao_itens",
                type: "bigint",
                nullable: true);

            // Volta ao possível: o custo da peça da biblioteca é o valor da cópia mais recente que tem valor, e o item
            // aponta para a peça da biblioteca vinculada. Itens de peças em rascunho (sem peça na biblioteca) se perdem.
            migrationBuilder.Sql("""
                UPDATE pecas p
                SET custo = x.valor
                FROM (
                    SELECT DISTINCT ON (peca_id) peca_id, valor
                    FROM projeto_pecas
                    WHERE peca_id IS NOT NULL AND valor IS NOT NULL
                    ORDER BY peca_id, id DESC
                ) x
                WHERE x.peca_id = p.id;

                UPDATE projeto_versao_itens i
                SET peca_id = pp.peca_id
                FROM projeto_pecas pp
                WHERE pp.id = i.peca_do_projeto_id;

                DELETE FROM projeto_versao_itens WHERE peca_id IS NULL;
                """);

            migrationBuilder.DropIndex(
                name: "ix_projeto_versao_itens_versao_id_peca_do_projeto_id",
                table: "projeto_versao_itens");

            migrationBuilder.DropIndex(
                name: "ix_projeto_versao_itens_peca_do_projeto_id",
                table: "projeto_versao_itens");

            migrationBuilder.DropColumn(
                name: "peca_do_projeto_id",
                table: "projeto_versao_itens");

            migrationBuilder.AlterColumn<long>(
                name: "peca_id",
                table: "projeto_versao_itens",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_projeto_versao_itens_peca_id",
                table: "projeto_versao_itens",
                column: "peca_id");

            migrationBuilder.CreateIndex(
                name: "ix_projeto_versao_itens_versao_id_peca_id",
                table: "projeto_versao_itens",
                columns: new[] { "versao_id", "peca_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_projeto_versao_itens_pecas_peca_id",
                table: "projeto_versao_itens",
                column: "peca_id",
                principalTable: "pecas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropTable(
                name: "projeto_pecas");
        }
    }
}
