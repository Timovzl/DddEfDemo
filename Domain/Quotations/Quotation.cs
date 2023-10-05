using Architect.DddEfDemo.DddEfDemo.Domain.Parties;

namespace Architect.DddEfDemo.DddEfDemo.Domain.Quotations;

/// <summary>
/// A formal offer of certain products at certain prices.
/// </summary>
public sealed class Quotation : Entity<QuotationId, decimal>
{
	public SellerId SellerId { get; }

	public DateTime CreationDateTime { get; }
	public DateTime? ExpirationDateTime { get; }

	// Convenience property - no database column please!
	public bool IsExpired => Clock.UtcNow >= this.ExpirationDateTime;

	public IReadOnlyList<QuotationLine> Lines { get; }

	// Convenience properties - no database relationships please!
	public IEnumerable<QuotationLine> PurchaseLines => this.Lines.Where(line => line.PricePerUnit >= 0m);
	public IEnumerable<QuotationLine> DiscountLines => this.Lines.Where(line => line.PricePerUnit < 0m);

	public Quotation(SellerId sellerId, DateTime? expirationDateTime, IEnumerable<QuotationLine> lines)
		: base(DistributedId.CreateId())
	{
		this.SellerId = sellerId;

		this.CreationDateTime = Clock.UtcNow;
		this.ExpirationDateTime = expirationDateTime;

		this.Lines = lines?.ToList() ?? throw new NullValidationException(ErrorCode.Quotation_LinesNull, nameof(lines));

		// If the following domain invariant were introduced later, can we still load existing empty quotations?
		//if (this.Lines.Count == 0)
		//	throw new ValidationException(ErrorCode.Quotation_LinesEmpty, $"A {nameof(Quotation)} must not be empty.");
	}
}
