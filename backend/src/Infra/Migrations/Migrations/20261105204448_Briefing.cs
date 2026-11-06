using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Castwork.Infra.Migrations
{
    /// <inheritdoc />
    public partial class Briefing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "briefings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    projeto_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_joia = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    o_que_o_cliente_quer = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ocasiao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    materiais = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    medidas = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    prazo = table.Column<DateOnly>(type: "date", nullable: true),
                    orcamento = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    o_que_sera_feito = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    acabamento = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    pedras = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    fecho = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<long>(type: "bigint", nullable: true),
                    alterado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    alterado_por = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_briefings", x => x.id);
                    table.ForeignKey(
                        name: "fk_briefings_projetos_projeto_id",
                        column: x => x.projeto_id,
                        principalTable: "projetos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "briefing_documentos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    briefing_id = table.Column<long>(type: "bigint", nullable: false),
                    nome_arquivo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    conteudo = table.Column<byte[]>(type: "bytea", nullable: false),
                    gerado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_briefing_documentos", x => x.id);
                    table.ForeignKey(
                        name: "fk_briefing_documentos_briefings_briefing_id",
                        column: x => x.briefing_id,
                        principalTable: "briefings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "briefing_mensagens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    papel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    texto = table.Column<string>(type: "text", nullable: true),
                    conteudo = table.Column<string>(type: "jsonb", nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    briefing_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_briefing_mensagens", x => x.id);
                    table.ForeignKey(
                        name: "fk_briefing_mensagens_briefings_briefing_id",
                        column: x => x.briefing_id,
                        principalTable: "briefings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_briefing_documentos_briefing_id_gerado_em",
                table: "briefing_documentos",
                columns: new[] { "briefing_id", "gerado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_briefing_mensagens_briefing_id_ordem",
                table: "briefing_mensagens",
                columns: new[] { "briefing_id", "ordem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_briefings_projeto_id",
                table: "briefings",
                column: "projeto_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "briefing_documentos");

            migrationBuilder.DropTable(
                name: "briefing_mensagens");

            migrationBuilder.DropTable(
                name: "briefings");
        }
    }
}
