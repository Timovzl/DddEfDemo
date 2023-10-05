using Architect.DddEfDemo.DddEfDemo.Domain.Products;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Mappings.Products;

internal sealed class ProductMapping : IEntityTypeConfiguration<Product>
{
	public void Configure(EntityTypeBuilder<Product> builder)
	{
		builder.Property(x => x.Id);

		builder.Property(x => x.CreationDateTime);

		builder.Property(x => x.ModificationDateTime);

		builder.Property(x => x.Name);

		// No can do!
		//builder.Property(x => x.Manufacturer.Name);
		//builder.Property(x => x.Manufacturer.EstablishedYear);

		// Map a flat ValueObject (simple set of values) to columns in our own table
		builder.OwnsOne(x => x.Manufacturer, details =>
		{
			details.Property(x => x.Name)
				.HasColumnName("ManufacturerName");

			details.Property(x => x.EstablishedYear)
				.HasColumnName("ManufacturerEst");
		});

		// Optimistic concurrency control
		builder.Property<byte[]>("RowVersion")
			.IsRequired()
			.IsRowVersion();

		builder.HasKey(x => x.Id);

		builder.HasIndex(x => x.CreationDateTime);

		builder.HasIndex(x => x.ModificationDateTime);
	}
}
