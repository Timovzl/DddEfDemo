namespace Architect.DddEfDemo.DddEfDemo.Domain.Shared;

/// <summary>
/// <para>
/// An ID value originating outside of the bounded context.
/// </para>
/// </summary>
[SourceGenerated]
public sealed partial class ExternalId : WrapperValueObject<string>, IIdentity<string>, IComparable<ExternalId>
{
	protected override StringComparison StringComparison => StringComparison.Ordinal;

	public const ushort MaxLength = 50;

	public string Value { get; }

	public ExternalId(string value)
	{
		this.Value = value ?? throw new NullValidationException(ErrorCode.ExternalId_ValueNull, nameof(value));

		if (this.Value.Length == 0)
			throw new ValidationException(ErrorCode.ExternalId_ValueEmpty, "An external ID value must not be empty.");
		if (this.Value.Length > MaxLength)
			throw new ValidationException(ErrorCode.ExternalId_ValueToolong, $"An external ID value must not be over {MaxLength} characters long.");
		if (ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters(this.Value) || this.Value.AsSpan().IndexOfAny('\'', '"') >= 0)
			throw new ValidationException(ErrorCode.ExternalId_ValueInvalid, "An external ID value must consist of printable, non-whitespace, non-quote ASCII characters.");
	}
}
