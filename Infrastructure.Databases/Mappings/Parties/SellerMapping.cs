using Architect.DddEfDemo.DddEfDemo.Domain.Parties;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Mappings.Parties;

internal sealed class SellerMapping : IEntityTypeConfiguration<Seller>
{
	public void Configure(EntityTypeBuilder<Seller> builder)
	{
		builder.Property(x => x.Id);

		builder.Property(x => x.CreationDateTime);

		builder.Property(x => x.ModificationDateTime);

		builder.Property(x => x.Description)
			.HasMaxLength(SellerDescription.MaxLength)
			.UseCollation(CoreDbContext.CulturalCollation);

		builder.Property(x => x.Name);

		// Optimistic concurrency control
		builder.Property<byte[]>("RowVersion")
			.IsRequired()
			.IsRowVersion();

		builder.HasKey(x => x.Id);

		builder.HasIndex(x => x.CreationDateTime);

		builder.HasIndex(x => x.ModificationDateTime);
	}
}
