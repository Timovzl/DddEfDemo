namespace Architect.DddEfDemo.DddEfDemo.Domain;

/// <summary>
/// <para>
/// The stable error codes defined by this bounded context.
/// </para>
/// <para>
/// Names are stable. Numeric values are meaningless.
/// </para>
/// <para>
/// DO NOT DELETE OR RENAME ITEMS.
/// </para>
/// </summary>
public enum ErrorCode
{
	// DO NOT DELETE OR RENAME ITEMS

	ExternalId_ValueNull,
	ExternalId_ValueEmpty,
	ExternalId_ValueToolong,
	ExternalId_ValueInvalid,

	ManufacturerDetails_NameNull,

	ProperName_ValueNull,
	ProperName_ValueTooShort,
	ProperName_ValueTooLong,
	ProperName_ValueInvalid,

	Seller_NameNull,
	Seller_DescriptionNull,

	SellerDescription_ValueNull,
	SellerDescription_ValueTooLong,
	SellerDescription_ValueInvalid,

	Product_NameNull,

	Quotation_LinesNull,
	Quotation_LinesEmpty,

	// DO NOT DELETE OR RENAME ITEMS
}
