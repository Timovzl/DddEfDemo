namespace Architect.DddEfDemo.DddEfDemo.Domain.Parties;

public sealed class Seller : Entity<SellerId, decimal>
{
	// Id is inherited

	public DateTime CreationDateTime { get; }
	public DateTime ModificationDateTime { get; private set; }

	public SellerDescription Description { get; private set; }
	public ProperName Name { get; private set; }

	public Seller(DateTime creationDateTime, SellerDescription description, ProperName name)
		: base(DistributedId.CreateId())
	{
		this.CreationDateTime = creationDateTime;
		this.ModificationDateTime = this.CreationDateTime;

		this.Description = description ?? throw new NullValidationException(ErrorCode.Seller_DescriptionNull, nameof(description));
		this.Name = name ?? throw new NullValidationException(ErrorCode.Seller_NameNull, nameof(name));
	}
}
