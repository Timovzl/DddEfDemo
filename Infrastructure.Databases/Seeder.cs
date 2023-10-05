using Architect.DddEfDemo.DddEfDemo.Domain.Products;
using Architect.DddEfDemo.DddEfDemo.Domain.Shared;
using Architect.Identities;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases;

/// <summary>
/// Seeds the database with initial data.
/// </summary>
public static class Seeder
{
	public static readonly decimal ProductOneId = 1m;
	public static readonly decimal ProductTwoId = 2m;

	public static void AddSeedData(ModelBuilder builder)
	{
		#region Create seed entities with pre-generated, fixed IDs, to be able to reference them

		var manufacturerBob = new ManufacturerDetails(new ProperName("Bob the Builder"), establishedYear: 1970);

		var products = new List<Product>();
		using (new DistributedIdGeneratorScope(new CustomDistributedIdGenerator(ProductOneId)))
			products.Add(new Product(DateTime.UnixEpoch, new ProperName("Product One"), manufacturerBob));
		using (new DistributedIdGeneratorScope(new CustomDistributedIdGenerator(ProductTwoId)))
			products.Add(new Product(DateTime.UnixEpoch, new ProperName("Product Two"), manufacturer: null));

		#endregion

		#region Work around issue where EF treats owned value objects as separate entities and wants to receive them separately for HasData()

		// To seed any ValueObject property values, use a workaround (https://github.com/dotnet/efcore/issues/10000)
		// EF requires the owned properties to be null on the parent, followed by a call to OwnsOne().HasData()
		// Hopefully, EF8's dedicated support for value objects will provide a better alternative
		var productManufacturers = products
			// For each Product, replace its Manufacturer by null and remember the previous value
			.Select(product => (Product: product, Manufacturer: typeof(Product).GetBackingField(nameof(Product.Manufacturer)).ExchangeValue<ManufacturerDetails>(product, value: null)))
			// Keep only Products that have a Manufacturer specified
			.Where(pair => pair.Manufacturer is not null)
			// Create an anonymous object containing each persisted property of the Manufacturer, plus the parent ProductdId
			.Select(pair => new
			{
				ProductId = pair.Product.Id,

				// WARNING: This is the one place where we duplicate property definitions, including their names
				// This BEGS for a better solution
				Name = pair.Manufacturer.Name,
				EstablishedYear = pair.Manufacturer.EstablishedYear,
			})
			.ToList();
		builder.Entity<Product>().OwnsOne(x => x.Manufacturer).HasData(productManufacturers);

		#endregion

		builder.Entity<Product>().HasData(products);
	}
}
