namespace Architect.DddEfDemo.DddEfDemo.Domain.Products;

public sealed class ManufacturerDetails : ValueObject
{
	public override string ToString() => $"{{{this.Name} est. {this.EstablishedYear}}}";

	public ProperName Name { get; }
	public ushort EstablishedYear { get; }

	public ManufacturerDetails(ProperName name, ushort establishedYear)
	{
		this.Name = name ?? throw new NullValidationException(ErrorCode.ManufacturerDetails_NameNull, nameof(name));
		this.EstablishedYear = establishedYear;
	}

	public ManufacturerDetails Clone()
	{
		return (ManufacturerDetails)this.MemberwiseClone();
	}
}
