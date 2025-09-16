using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace Magnar.AI.Infrastructure.Configurations
{
    public class DataProtectionKeysConfiguration : IEntityTypeConfiguration<DataProtectionKey>
    {
        public void Configure(EntityTypeBuilder<DataProtectionKey> builder)
        {
            builder.ToTable(Constants.Database.TableNames.DataProtectionKeys, Constants.Database.Schemas.Identity);
        }
    }

}
