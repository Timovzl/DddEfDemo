namespace Architect.DddEfDemo.DddEfDemo.Domain.Parties;

[SourceGenerated]
public sealed partial class SellerDescription : WrapperValueObject<string>
{
	protected override StringComparison StringComparison => StringComparison.OrdinalIgnoreCase;

	public const ushort MaxLength = 4000;

	public string Value { get; }

	public SellerDescription(string value)
	{
		this.Value = value ?? throw new NullValidationException(ErrorCode.SellerDescription_ValueNull, nameof(SellerDescription));

		if (this.Value.Length > MaxLength)
			throw new ValidationException(ErrorCode.SellerDescription_ValueTooLong, $"A {nameof(SellerDescription)} must not be over {MaxLength} characters long.");
		if (ContainsNonPrintableCharacters(this.Value, flagNewLinesAndTabs: false))
			throw new ValidationException(ErrorCode.SellerDescription_ValueInvalid, $"A {nameof(SellerDescription)} must contain only printable characters.");
	}
}
