using Architect.DddEfDemo.DddEfDemo.Domain.Products;
using Architect.DddEfDemo.DddEfDemo.Domain.Quotations;
using Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases;
using Architect.DomainModeling;

namespace Architect.DddEfDemo.DddEfDemo.Testing.Common.Builders;

[SourceGenerated]
public sealed partial class QuotationDummyBuilder : DummyBuilder<Quotation, QuotationDummyBuilder>
{
	private IEnumerable<QuotationLine> Lines { get; set; } = new[]
	{
		new QuotationLine(new ProductId(Seeder.ProductOneId), quantity: 1, pricePerUnit: 1.0m),
		new QuotationLine(new ProductId(Seeder.ProductTwoId), quantity: 2, pricePerUnit: 2.0m),
	};
}
