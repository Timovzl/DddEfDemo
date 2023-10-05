using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Shared.Conventions;

/// <summary>
/// A convention that uses string comparisons with case-sensitivity matching each relevant column's collation, for every property mapped to a string column.
/// This helps align comparisons with those made by the database.
/// </summary>
internal sealed class StringCasingConvention : IModelFinalizingConvention
{
	private static readonly ValueComparer OrdinalComparer = new ValueComparer<string>(
		equalsExpression: (left, right) => String.Equals(left, right, StringComparison.Ordinal),
		hashCodeExpression: value => String.GetHashCode(value, StringComparison.Ordinal),
		snapshotExpression: value => value);

	private static readonly ValueComparer OrdinalIgnoreCaseComparer = new ValueComparer<string>(
		equalsExpression: (left, right) => String.Equals(left, right, StringComparison.OrdinalIgnoreCase),
		hashCodeExpression: value => String.GetHashCode(value, StringComparison.OrdinalIgnoreCase),
		snapshotExpression: value => value);

	public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
	{
		foreach (var property in modelBuilder.Metadata.GetEntityTypes().SelectMany(entityBuilder => entityBuilder.GetProperties()))
		{
			// Either a plain string property or a property mapped to string
			if (property.ClrType != typeof(string) && property.GetValueConverter()?.ProviderClrType != typeof(string))
				continue;

			var collation = property.FindAnnotation(RelationalAnnotationNames.Collation) ??
				modelBuilder.Metadata.FindAnnotation(RelationalAnnotationNames.Collation);

			// Use case-sensitive comparisons unless ignore-case is explicitly used by the collation
			var comparer = collation?.Value is string collationName && collationName.Contains("_CI", StringComparison.OrdinalIgnoreCase)
				? OrdinalIgnoreCaseComparer
				: OrdinalComparer;

			// We already confirmed that the provider type is string
			if (property.GetProviderValueComparer() is null)
				property.SetProviderValueComparer(comparer, fromDataAnnotation: true); // Using fromDataAnnotation=true allows explicit configuration to override ours

			// The model type COULD be string
			if (property.ClrType == typeof(string) && property.GetValueComparer() is null)
				property.SetValueComparer(comparer, fromDataAnnotation: true); // Using fromDataAnnotation=true allows explicit configuration to override ours
		}
	}
}
