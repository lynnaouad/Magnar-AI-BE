using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magnar.AI.Infrastructure.Configurations
{
    public class ApiProviderDetailsConfiguration : IEntityTypeConfiguration<ApiProviderDetails>
    {
        public void Configure(EntityTypeBuilder<ApiProviderDetails> builder)
        {
            builder.HasOne(x => x.Provider).WithMany(x => x.ApiProviderDetails).HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
