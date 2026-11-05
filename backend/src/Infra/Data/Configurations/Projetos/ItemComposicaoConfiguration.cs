using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Projetos;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Projetos;

internal sealed class ItemComposicaoConfiguration : IEntityTypeConfiguration<ItemComposicao>
{
    public void Configure(EntityTypeBuilder<ItemComposicao> builder)
    {
        builder.ToTable("projeto_versao_itens");
        builder.HasKey(i => i.Id);
        builder.IgnorarEventos();

        // FK para a versão: propriedade de sombra (o domínio não a expõe), declarada aqui para o índice abaixo.
        builder.Property<long>("VersaoId");
        builder.Property(i => i.Quantidade).IsRequired();
        builder.Property(i => i.Observacao).HasMaxLength(ItemComposicao.ObservacaoMaxLength);

        // Peça usada em alguma composição não pode ser excluída (vira inativa).
        builder.HasOne<Peca>().WithMany().HasForeignKey(i => i.PecaId).OnDelete(DeleteBehavior.Restrict);

        // Uma peça aparece uma vez por versão; repetir soma a quantidade (regra do domínio).
        builder.HasIndex("VersaoId", nameof(ItemComposicao.PecaId)).IsUnique();
    }
}
