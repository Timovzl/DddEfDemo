using Architect.DddEfDemo.DddEfDemo.Domain.Products;

namespace Architect.DddEfDemo.DddEfDemo.Domain.Quotations;

/// <summary>
/// An offer of a single item, usually as part of a quotation.
/// </summary>
[SourceGenerated]
public sealed partial class QuotationLine : ValueObject
{
	public override string ToString() => $"{{{nameof(QuotationLine)} Product={this.ProductId} Qty={this.Quantity} PPU={this.PricePerUnit:N2}}}";

	public ProductId ProductId { get; }
	public uint Quantity { get; }
	public decimal PricePerUnit { get; }

	public QuotationLine(ProductId productId, uint quantity, decimal pricePerUnit)
	{
		this.ProductId = productId;
		this.Quantity = quantity;
		this.PricePerUnit = pricePerUnit;
	}
}
