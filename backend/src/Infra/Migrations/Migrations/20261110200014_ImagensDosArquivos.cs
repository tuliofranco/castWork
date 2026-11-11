using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Castwork.Infra.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ImagensDosArquivos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ferramenta_arquivo_imagens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    arquivo_id = table.Column<long>(type: "bigint", nullable: false),
                    vista = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    conteudo = table.Column<byte[]>(type: "bytea", nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ferramenta_arquivo_imagens", x => x.id);
                    table.ForeignKey(
                        name: "fk_ferramenta_arquivo_imagens_ferramenta_arquivos_arquivo_id",
                        column: x => x.arquivo_id,
                        principalTable: "ferramenta_arquivos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "peca_desenho_imagens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vista = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    conteudo = table.Column<byte[]>(type: "bytea", nullable: false),
                    desenho_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_peca_desenho_imagens", x => x.id);
                    table.ForeignKey(
                        name: "fk_peca_desenho_imagens_peca_desenhos_desenho_id",
                        column: x => x.desenho_id,
                        principalTable: "peca_desenhos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ferramenta_arquivo_imagens_arquivo_id_vista_tipo",
                table: "ferramenta_arquivo_imagens",
                columns: new[] { "arquivo_id", "vista", "tipo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_peca_desenho_imagens_desenho_id_vista_tipo",
                table: "peca_desenho_imagens",
                columns: new[] { "desenho_id", "vista", "tipo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ferramenta_arquivo_imagens");

            migrationBuilder.DropTable(
                name: "peca_desenho_imagens");
        }
    }
}
