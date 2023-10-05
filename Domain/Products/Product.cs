namespace Architect.DddEfDemo.DddEfDemo.Domain.Products;

public sealed class Product : Entity<ProductId, decimal>
{
	public override string ToString() => $"{{{nameof(Product)} Id={this.Id} Name={this.Name}}}";

	// Id is inherited

	public DateTime CreationDateTime { get; }
	public DateTime ModificationDateTime { get; private set; }

	public ProperName Name { get; }

	public ManufacturerDetails? Manufacturer { get; }

	#region Optional workaround for OwnsOne() "same reference" issue
	//public ManufacturerDetails? Manufacturer
	//{
	//	get => this._manufacturer;
	//	private set => this._manufacturer = value?.Clone();
	//}
	//private ManufacturerDetails? _manufacturer;
	#endregion

	public Product(DateTime creationDateTime, ProperName name, ManufacturerDetails? manufacturer)
		: base(DistributedId.CreateId())
	{
		this.CreationDateTime = creationDateTime;
		this.ModificationDateTime = this.CreationDateTime;

		this.Name = name ?? throw new NullValidationException(ErrorCode.Product_NameNull, nameof(name));
		this.Manufacturer = manufacturer;
	}
}
