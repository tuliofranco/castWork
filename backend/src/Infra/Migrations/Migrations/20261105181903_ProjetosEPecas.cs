using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Castwork.Infra.Migrations
{
    /// <inheritdoc />
    public partial class ProjetosEPecas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pecas_clientes_cliente_id",
                table: "pecas");

            migrationBuilder.DropTable(
                name: "peca_etapas");

            migrationBuilder.DropTable(
                name: "peca_observacoes");

            migrationBuilder.DropIndex(
                name: "ix_pecas_cliente_id",
                table: "pecas");

            migrationBuilder.DropIndex(
                name: "ix_pecas_prazo_entrega",
                table: "pecas");

            migrationBuilder.DropIndex(
                name: "ix_pecas_status",
                table: "pecas");

            migrationBuilder.DropColumn(
                name: "cliente_id",
                table: "pecas");

            migrationBuilder.DropColumn(
                name: "entregue_em",
                table: "pecas");

            migrationBuilder.DropColumn(
                name: "motivo_cancelamento",
                table: "pecas");

            migrationBuilder.DropColumn(
                name: "prazo_entrega",
                table: "pecas");

            migrationBuilder.DropColumn(
                name: "status",
                table: "pecas");

            migrationBuilder.RenameColumn(
                name: "titulo",
                table: "pecas",
                newName: "nome");

            migrationBuilder.AlterColumn<string>(
                name: "descricao",
                table: "pecas",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ativo",
                table: "pecas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "material",
                table: "pecas",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "peso",
                table: "pecas",
                type: "numeric(10,3)",
                precision: 10,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "teor",
                table: "pecas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo",
                table: "pecas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "projetos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    cliente_id = table.Column<long>(type: "bigint", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<long>(type: "bigint", nullable: true),
                    alterado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    alterado_por = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projetos", x => x.id);
                    table.ForeignKey(
                        name: "fk_projetos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "projeto_versoes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    publicada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    projeto_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projeto_versoes", x => x.id);
                    table.ForeignKey(
                        name: "fk_projeto_versoes_projetos_projeto_id",
                        column: x => x.projeto_id,
                        principalTable: "projetos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "projeto_versao_itens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    peca_id = table.Column<long>(type: "bigint", nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    versao_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projeto_versao_itens", x => x.id);
                    table.ForeignKey(
                        name: "fk_projeto_versao_itens_pecas_peca_id",
                        column: x => x.peca_id,
                        principalTable: "pecas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_projeto_versao_itens_projeto_versoes_versao_id",
                        column: x => x.versao_id,
                        principalTable: "projeto_versoes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pecas_nome",
                table: "pecas",
                column: "nome");

            migrationBuilder.CreateIndex(
                name: "ix_pecas_tipo",
                table: "pecas",
                column: "tipo");

            migrationBuilder.CreateIndex(
                name: "ix_projeto_versao_itens_peca_id",
                table: "projeto_versao_itens",
                column: "peca_id");

            migrationBuilder.CreateIndex(
                name: "ix_projeto_versao_itens_versao_id_peca_id",
                table: "projeto_versao_itens",
                columns: new[] { "versao_id", "peca_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_projeto_versoes_projeto_id_numero",
                table: "projeto_versoes",
                columns: new[] { "projeto_id", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_projetos_cliente_id",
                table: "projetos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_projetos_nome",
                table: "projetos",
                column: "nome");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "projeto_versao_itens");

            migrationBuilder.DropTable(
                name: "projeto_versoes");

            migrationBuilder.DropTable(
                name: "projetos");

            migrationBuilder.DropIndex(
                name: "ix_pecas_nome",
                table: "pecas");

            migrationBuilder.DropIndex(
                name: "ix_pecas_tipo",
                table: "pecas");

            migrationBuilder.DropColumn(
                name: "ativo",
                table: "pecas");

            migrationBuilder.DropColumn(
                name: "material",
                table: "pecas");

            migrationBuilder.DropColumn(
                name: "peso",
                table: "pecas");

            migrationBuilder.DropColumn(
                name: "teor",
                table: "pecas");

            migrationBuilder.DropColumn(
                name: "tipo",
                table: "pecas");

            migrationBuilder.RenameColumn(
                name: "nome",
                table: "pecas",
                newName: "titulo");

            migrationBuilder.AlterColumn<string>(
                name: "descricao",
                table: "pecas",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "cliente_id",
                table: "pecas",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "entregue_em",
                table: "pecas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_cancelamento",
                table: "pecas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "prazo_entrega",
                table: "pecas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "pecas",
                type: "character(1)",
                fixedLength: true,
                maxLength: 1,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "peca_etapas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    concluida_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    iniciada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    peca_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character(1)", fixedLength: true, maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_peca_etapas", x => x.id);
                    table.ForeignKey(
                        name: "fk_peca_etapas_pecas_peca_id",
                        column: x => x.peca_id,
                        principalTable: "pecas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "peca_observacoes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    peca_id = table.Column<long>(type: "bigint", nullable: false),
                    status_da_peca = table.Column<string>(type: "character(1)", fixedLength: true, maxLength: 1, nullable: false),
                    texto = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_peca_observacoes", x => x.id);
                    table.ForeignKey(
                        name: "fk_peca_observacoes_pecas_peca_id",
                        column: x => x.peca_id,
                        principalTable: "pecas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pecas_cliente_id",
                table: "pecas",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_pecas_prazo_entrega",
                table: "pecas",
                column: "prazo_entrega");

            migrationBuilder.CreateIndex(
                name: "ix_pecas_status",
                table: "pecas",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_peca_etapas_peca_id_iniciada_em",
                table: "peca_etapas",
                columns: new[] { "peca_id", "iniciada_em" });

            migrationBuilder.CreateIndex(
                name: "ix_peca_observacoes_peca_id_criada_em",
                table: "peca_observacoes",
                columns: new[] { "peca_id", "criada_em" });

            migrationBuilder.AddForeignKey(
                name: "fk_pecas_clientes_cliente_id",
                table: "pecas",
                column: "cliente_id",
                principalTable: "clientes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
