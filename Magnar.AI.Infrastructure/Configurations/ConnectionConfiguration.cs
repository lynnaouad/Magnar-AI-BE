using Magnar.AI.Domain.Static;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Magnar.AI.Infrastructure.Configurations
{
    public class ConnectionConfiguration : IEntityTypeConfiguration<Connection>
    {
        public void Configure(EntityTypeBuilder<Connection> builder)
        {
            builder.Property(x => x.Provider).HasConversion(new EnumToStringConverter<ProviderTypes>());

            builder.HasIndex(x => x.IsDefault).HasFilter("[IsDefault] = 1").IsUnique();

        }
    }
}
