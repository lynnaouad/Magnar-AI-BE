using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magnar.AI.Infrastructure.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable(Constants.Database.TableNames.Users, Constants.Database.Schemas.Identity);
    }
}