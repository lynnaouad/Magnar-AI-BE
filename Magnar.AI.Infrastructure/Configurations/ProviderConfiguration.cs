using Magnar.AI.Domain.Static;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Magnar.AI.Infrastructure.Configurations
{
    public class ProviderConfiguration : IEntityTypeConfiguration<Provider>
    {
        public void Configure(EntityTypeBuilder<Provider> builder)
        {
            builder.Property(x => x.Type).HasConversion(new EnumToStringConverter<ProviderTypes>());

            builder.Property(x => x.IsDefault).IsRequired().HasDefaultValue(false);

            builder.HasIndex(x => new { x.Type, x.IsDefault }).IsUnique().HasFilter("[IsDefault] = 1");

            builder.HasOne(x => x.Workspace).WithMany().HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
