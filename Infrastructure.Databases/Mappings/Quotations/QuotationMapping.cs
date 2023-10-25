using System.Text.Json;
using Architect.DddEfDemo.DddEfDemo.Domain.Products;
using Architect.DddEfDemo.DddEfDemo.Domain.Quotations;
using Architect.DomainModeling.Comparisons;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Mappings.Quotations;

internal sealed class QuotationMapping : IEntityTypeConfiguration<Quotation>
{
	public void Configure(EntityTypeBuilder<Quotation> builder)
	{
		builder.Property(x => x.Id);

		builder.Property(x => x.SellerId);

		builder.Property(x => x.CreationDateTime);

		builder.Property(x => x.ExpirationDateTime);

		// Luckily, our conventions can have simple properties like IsExpired ignored by default
		// Sadly, navigation properties like PurchaseLines are discovered unless we ignore them explicitly

		builder.Ignore(x => x.PurchaseLines);

		builder.Ignore(x => x.DiscountLines);

		// Map a collection of value objects to a JSON blob
		// Use a structural comparer for equality checks
		// Note that the alternative of OwnsOne(x => x.Lines, linesBuilder => linesBuilder.ToJson()) will NOT compare correctly
		// However, the future alternative of ComplexType(x => x.Lines, linesBuilder => linesBuilder.ToJson()) should compare correctly, allow serializer specification, and allow collation specification
		// Regardless, ToJson() is rarely useful for a collection, since querying into contents of a collection stored in a blob tends to be inefficient and overly specificc
		builder.Property(x => x.Lines)
			.HasConversion(
				codeValue => JsonSerializer.Serialize(codeValue, (JsonSerializerOptions?)null),
				dbValue => JsonSerializer.Deserialize<List<QuotationLine>>(dbValue, (JsonSerializerOptions?)null)!,
				new ValueComparer<IReadOnlyList<QuotationLine>>(
					equalsExpression: (left, right) => EnumerableComparer.EnumerableEquals(left, right),
					hashCodeExpression: lines => EnumerableComparer.GetEnumerableHashCode(lines)))
			.UseCollation(CoreDbContext.BinaryCollation);

		#region Alternative for mapping to separate table

		/*
		// Map a collection of child ValueObjects to a separate table
		// When querying, they will be included by default
		builder.OwnsMany(quotation => quotation.Lines, line =>
		{
			line.ToTable("QuotationLines");

			// Shadow PK
			line.Property<long>("Id")
				.ValueGeneratedOnAdd();

			// Shadow parent ID
			line.Property<QuotationId>("QuotationId");

			// Actual properties

			line.Property(x => x.ProductId);

			line.Property(x => x.Quantity);

			line.Property(x => x.PricePerUnit);

			// Indexes

			line.HasKey("Id");

			line.WithOwner().HasForeignKey("QuotationId");

			// Manually declare a foreign key, for lack of a navigation property on either end
			line.HasOne<Product>().WithMany().HasForeignKey(line => line.ProductId)
				.OnDelete(DeleteBehavior.Restrict);
		});
		*/

		#endregion

		// Optimistic concurrency control
		builder.Property<byte[]>("RowVersion")
			.IsRequired()
			.IsRowVersion();

		builder.HasKey(x => x.Id);

		builder.HasIndex(x => x.CreationDateTime);

		builder.HasIndex(x => x.ExpirationDateTime);
	}
}
