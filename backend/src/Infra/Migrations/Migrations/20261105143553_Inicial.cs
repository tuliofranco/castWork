using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Castwork.Infra.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    documento = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<long>(type: "bigint", nullable: true),
                    alterado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    alterado_por = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    endereco_bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    endereco_cep = table.Column<string>(type: "character(8)", fixedLength: true, maxLength: 8, nullable: true),
                    endereco_cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    endereco_complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    endereco_logradouro = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    endereco_numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    endereco_uf = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pecas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cliente_id = table.Column<long>(type: "bigint", nullable: false),
                    titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descricao = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    status = table.Column<string>(type: "character(1)", fixedLength: true, maxLength: 1, nullable: false),
                    prazo_entrega = table.Column<DateOnly>(type: "date", nullable: true),
                    entregue_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    motivo_cancelamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<long>(type: "bigint", nullable: true),
                    alterado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    alterado_por = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pecas", x => x.id);
                    table.ForeignKey(
                        name: "fk_pecas_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "peca_etapas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    status = table.Column<string>(type: "character(1)", fixedLength: true, maxLength: 1, nullable: false),
                    iniciada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concluida_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    peca_id = table.Column<long>(type: "bigint", nullable: false)
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
                    texto = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status_da_peca = table.Column<string>(type: "character(1)", fixedLength: true, maxLength: 1, nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    peca_id = table.Column<long>(type: "bigint", nullable: false)
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
                name: "ix_clientes_documento",
                table: "clientes",
                column: "documento",
                unique: true,
                filter: "documento IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_clientes_nome",
                table: "clientes",
                column: "nome");

            migrationBuilder.CreateIndex(
                name: "ix_peca_etapas_peca_id_iniciada_em",
                table: "peca_etapas",
                columns: new[] { "peca_id", "iniciada_em" });

            migrationBuilder.CreateIndex(
                name: "ix_peca_observacoes_peca_id_criada_em",
                table: "peca_observacoes",
                columns: new[] { "peca_id", "criada_em" });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "peca_etapas");

            migrationBuilder.DropTable(
                name: "peca_observacoes");

            migrationBuilder.DropTable(
                name: "pecas");

            migrationBuilder.DropTable(
                name: "clientes");
        }
    }
}
