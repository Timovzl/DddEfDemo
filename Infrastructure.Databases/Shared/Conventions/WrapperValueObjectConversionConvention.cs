using Architect.DomainModeling;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Shared.Conventions;

/// <summary>
/// A convention that uses <see cref="WrapperConverter{TModel, TProvider}"/> to convert types inheriting from <see cref="WrapperValueObject{TValue}"/>.
/// </summary>
internal sealed class WrapperValueObjectConversionConvention : IPropertyAddedConvention
{
	public void ProcessPropertyAdded(IConventionPropertyBuilder propertyBuilder, IConventionContext<IConventionPropertyBuilder> context)
	{
		var baseType = propertyBuilder.Metadata.ClrType.BaseType;

		// Only wrapper value objects
		if (baseType?.IsGenericType != true || baseType.GetGenericTypeDefinition() != typeof(WrapperValueObject<>))
			return;

		var modelType = propertyBuilder.Metadata.ClrType;
		var providerType = baseType.GenericTypeArguments[0];

		propertyBuilder.Metadata.SetValueConverter(typeof(WrapperConverter<,>).MakeGenericType(modelType, providerType), fromDataAnnotation: true); // Using fromDataAnnotation=true allows explicit configuration to override ours

		// A note on comparisons:
		// For non-key properties, EF uses the WrapperValueObject's own comparison methods, which the developer should have aligned with the column type and collation
		// For properties configured as database keys, EF compares the PROVIDER values, i.e. the underlying primitives (https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-7.0/breaking-changes#provider-value-comparer)
		// The latter comparison is assumed to be correct by default, except for strings, which are covered by the StringCasingConvention
	}
}
