using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magnar.AI.Infrastructure.Configurations
{
    public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
    {
        public void Configure(EntityTypeBuilder<ApiKey> builder)
        {
            builder.HasIndex(k => k.PublicId).IsUnique();

            builder.Property(k => k.PublicId).HasMaxLength(60);

            builder.Property(k => k.Hash).HasMaxLength(200);

            builder.Property(k => k.TenantId).HasMaxLength(100);

            builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
