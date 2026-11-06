using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Castwork.Infra.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class BriefingComoFerramenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ferramenta_conversas_versao_id_ferramenta",
                table: "ferramenta_conversas");

            migrationBuilder.AlterColumn<long>(
                name: "versao_id",
                table: "ferramenta_conversas",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "projeto_id",
                table: "ferramenta_conversas",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // As conversas que já existem são de uma versão: o projeto vem dela.
            migrationBuilder.Sql("""
                UPDATE ferramenta_conversas c
                SET projeto_id = v.projeto_id
                FROM projeto_versoes v
                WHERE v.id = c.versao_id;
                """);

            // A conversa do briefing vira a conversa da ferramenta "briefing" do projeto (chat 1), com as
            // mensagens e as imagens como estavam. O conteúdo já está no mesmo formato de blocos.
            migrationBuilder.Sql("""
                INSERT INTO ferramenta_conversas (projeto_id, versao_id, ferramenta, criada_em, chat_atual)
                SELECT b.projeto_id, NULL, 'briefing', MIN(m.criada_em), 1
                FROM briefings b
                JOIN briefing_mensagens m ON m.briefing_id = b.id
                GROUP BY b.projeto_id;

                INSERT INTO ferramenta_mensagens (conversa_id, ordem, chat, papel, texto, conteudo, imagens, criada_em, acao_desfeita, passos)
                SELECT c.id, m.ordem, 1, m.papel, m.texto, m.conteudo, m.imagens, m.criada_em, FALSE, '{}'
                FROM briefing_mensagens m
                JOIN briefings b ON b.id = m.briefing_id
                JOIN ferramenta_conversas c ON c.projeto_id = b.projeto_id AND c.versao_id IS NULL AND c.ferramenta = 'briefing';

                INSERT INTO ferramenta_imagens (conversa_id, chave, tipo, conteudo, criada_em)
                SELECT c.id, i.chave, i.tipo, i.conteudo, i.enviada_em
                FROM briefing_imagens i
                JOIN briefings b ON b.id = i.briefing_id
                JOIN ferramenta_conversas c ON c.projeto_id = b.projeto_id AND c.versao_id IS NULL AND c.ferramenta = 'briefing';
                """);

            migrationBuilder.DropTable(
                name: "briefing_imagens");

            migrationBuilder.DropTable(
                name: "briefing_mensagens");

            migrationBuilder.CreateIndex(
                name: "ix_ferramenta_conversas_projeto_id_versao_id_ferramenta",
                table: "ferramenta_conversas",
                columns: new[] { "projeto_id", "versao_id", "ferramenta" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_ferramenta_conversas_versao_id",
                table: "ferramenta_conversas",
                column: "versao_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ferramenta_conversas_projetos_projeto_id",
                table: "ferramenta_conversas",
                column: "projeto_id",
                principalTable: "projetos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ferramenta_conversas_projetos_projeto_id",
                table: "ferramenta_conversas");

            migrationBuilder.DropIndex(
                name: "ix_ferramenta_conversas_projeto_id_versao_id_ferramenta",
                table: "ferramenta_conversas");

            migrationBuilder.DropIndex(
                name: "ix_ferramenta_conversas_versao_id",
                table: "ferramenta_conversas");

            migrationBuilder.DropColumn(
                name: "projeto_id",
                table: "ferramenta_conversas");

            migrationBuilder.AlterColumn<long>(
                name: "versao_id",
                table: "ferramenta_conversas",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "briefing_imagens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    briefing_id = table.Column<long>(type: "bigint", nullable: false),
                    chave = table.Column<Guid>(type: "uuid", nullable: false),
                    conteudo = table.Column<byte[]>(type: "bytea", nullable: false),
                    enviada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
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

            migrationBuilder.CreateTable(
                name: "briefing_mensagens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    briefing_id = table.Column<long>(type: "bigint", nullable: false),
                    conteudo = table.Column<string>(type: "jsonb", nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    imagens = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    papel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    texto = table.Column<string>(type: "text", nullable: true)
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
                name: "ix_ferramenta_conversas_versao_id_ferramenta",
                table: "ferramenta_conversas",
                columns: new[] { "versao_id", "ferramenta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_briefing_imagens_briefing_id",
                table: "briefing_imagens",
                column: "briefing_id");

            migrationBuilder.CreateIndex(
                name: "ix_briefing_imagens_chave",
                table: "briefing_imagens",
                column: "chave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_briefing_mensagens_briefing_id_ordem",
                table: "briefing_mensagens",
                columns: new[] { "briefing_id", "ordem" },
                unique: true);
        }
    }
}
